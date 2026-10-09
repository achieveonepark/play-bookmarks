using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Achieve.PlayBookmarks.Editor
{
    /// <summary>Versioned, project-portable .pbm JSON. Steps store explicit type keys and plain serialized data.</summary>
    public static class BookmarkFileCodec
    {
        private const int MaxCharacters = 1024 * 1024;
        [Serializable] private sealed class Document
        {
            public int formatVersion;
            public string description;
            public string sceneGuid;
            public string startScenePath;
            public bool useRandomSeed;
            public int randomSeed;
            public string readySignalId;
            public float readyTimeoutSeconds;
            public List<StepData> steps;
        }
        [Serializable] private sealed class StepData { public string type; public string json; }

        public static string ToJson(BookmarkProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (profile.steps == null || profile.steps.Count > 256) throw new ArgumentException("A bookmark supports at most 256 steps.");
            RejectObjectReferences(profile);
            var document = new Document
            {
                formatVersion = 1,
                description = profile.description,
                sceneGuid = AssetDatabase.AssetPathToGUID(profile.startScenePath ?? string.Empty),
                startScenePath = profile.startScenePath,
                useRandomSeed = profile.useRandomSeed,
                randomSeed = profile.randomSeed,
                readySignalId = profile.readySignalId,
                readyTimeoutSeconds = profile.readyTimeoutSeconds,
                steps = new List<StepData>()
            };
            foreach (var step in profile.steps)
            {
                if (step == null) throw new ArgumentException("Cannot save a null step.");
                if (step is MissingBookmarkStep missing)
                    document.steps.Add(new StepData { type = missing.missingType, json = missing.originalJson });
                else document.steps.Add(new StepData { type = TypeKey(step.GetType()), json = JsonUtility.ToJson(step) });
            }
            var json = JsonUtility.ToJson(document, true);
            if (json.Length > MaxCharacters) throw new ArgumentException("Bookmark exceeds the 1M character limit.");
            return json;
        }

        public static BookmarkProfile FromJson(string json, string name = "Bookmark")
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > MaxCharacters)
                throw new ArgumentException("Bookmark is empty or exceeds 1M characters.");
            Document document;
            try { document = JsonUtility.FromJson<Document>(json); }
            catch (Exception exception) { throw new ArgumentException("Bookmark JSON is invalid.", exception); }
            if (document == null || document.formatVersion != 1 || document.steps == null || document.steps.Count > 256)
                throw new ArgumentException("Unsupported or incomplete .pbm document.");
            var profile = ScriptableObject.CreateInstance<BookmarkProfile>();
            try
            {
                profile.name = name;
                profile.description = document.description;
                var resolvedScene = string.IsNullOrEmpty(document.sceneGuid) ? string.Empty : AssetDatabase.GUIDToAssetPath(document.sceneGuid);
                profile.startScenePath = string.IsNullOrEmpty(resolvedScene) ? document.startScenePath : resolvedScene;
                profile.useRandomSeed = document.useRandomSeed;
                profile.randomSeed = document.randomSeed;
                profile.readySignalId = document.readySignalId;
                profile.readyTimeoutSeconds = document.readyTimeoutSeconds;
                var types = TypeCache.GetTypesDerivedFrom<BookmarkStep>()
                    .Where(type => !type.IsAbstract && type.IsSerializable && !type.ContainsGenericParameters)
                    .ToDictionary(TypeKey, type => type);
                foreach (var data in document.steps)
                {
                    if (data == null || string.IsNullOrWhiteSpace(data.type) || string.IsNullOrWhiteSpace(data.json))
                        throw new ArgumentException("Bookmark contains an incomplete step.");
                    if (!types.TryGetValue(data.type, out var type))
                    {
                        profile.steps.Add(new MissingBookmarkStep { missingType = data.type, originalJson = data.json });
                        continue;
                    }
                    var step = JsonUtility.FromJson(data.json, type) as BookmarkStep;
                    if (step == null) throw new ArgumentException($"Could not deserialize step '{data.type}'.");
                    profile.steps.Add(step);
                }
                return profile;
            }
            catch { UnityEngine.Object.DestroyImmediate(profile); throw; }
        }

        public static BookmarkProfile Read(string path)
        {
            if (new FileInfo(path).Length > 4 * MaxCharacters) throw new ArgumentException("Bookmark file is too large.");
            return FromJson(File.ReadAllText(path, Encoding.UTF8), Path.GetFileNameWithoutExtension(path));
        }

        public static void Write(string path, BookmarkProfile profile)
        {
            var json = ToJson(profile);
            var temporary = path + ".writing";
            try
            {
                File.WriteAllText(temporary, json + "\n", new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        private static string TypeKey(Type type) => type.FullName + ", " + type.Assembly.GetName().Name;

        private static void RejectObjectReferences(BookmarkProfile profile)
        {
            using (var data = new SerializedObject(profile))
            {
                var iterator = data.FindProperty("steps").Copy();
                var end = iterator.GetEndProperty();
                while (iterator.Next(true) && !SerializedProperty.EqualContents(iterator, end))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue != null)
                        throw new ArgumentException(".pbm steps store plain data. Use target IDs or asset GUIDs instead of Unity object references.");
            }
        }
    }
}
