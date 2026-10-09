using System;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Achieve.PlayBookmarks.Editor
{
    [ScriptedImporter(1, "pbm")]
    public sealed class BookmarkFileImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            try
            {
                var profile = BookmarkFileCodec.Read(context.assetPath);
                context.AddObjectToAsset("bookmark", profile);
                context.SetMainObject(profile);
                if (!string.IsNullOrEmpty(profile.startScenePath) && File.Exists(profile.startScenePath))
                    context.DependsOnSourceAsset(profile.startScenePath);
                foreach (var step in profile.steps)
                    if (step is MissingBookmarkStep missing) context.LogImportWarning(missing.Validate());
            }
            catch (Exception exception)
            {
                context.LogImportError("Could not import bookmark: " + exception.Message);
                var error = new TextAsset(exception.Message) { name = "Invalid bookmark" };
                context.AddObjectToAsset("error", error);
                context.SetMainObject(error);
            }
        }
    }
}
