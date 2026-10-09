using System;
using System.Collections.Generic;
using UnityEngine;

namespace Achieve.PlayBookmarks
{
    public sealed class BookmarkProfile : ScriptableObject
    {
        [TextArea] public string description;
        [Tooltip("Project-relative scene path. Used by the Editor launcher; runtime Run uses the current scene.")]
        public string startScenePath;
        public bool useRandomSeed;
        public int randomSeed = 1234;
        [Tooltip("Optional BookmarkReadySignal ID to wait for before applying steps.")]
        public string readySignalId;
        [Min(0.01f)] public float readyTimeoutSeconds = 30f;
        [SerializeReference] public List<BookmarkStep> steps = new List<BookmarkStep>();

        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (!IsPositiveFinite(readyTimeoutSeconds))
                errors.Add("Ready timeout must be a positive finite number.");
            if (steps == null)
            {
                errors.Add("Steps list is missing.");
                return errors;
            }
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step == null)
                {
                    errors.Add($"Step {i + 1} is missing (its type may have been removed).");
                    continue;
                }
                if (!step.enabled) continue;
                if (!IsPositiveFinite(step.timeoutSeconds))
                    errors.Add($"Step {i + 1}: timeout must be a positive finite number.");
                try
                {
                    var error = step.Validate();
                    if (!string.IsNullOrEmpty(error)) errors.Add($"Step {i + 1}: {error}");
                }
                catch (Exception exception)
                {
                    errors.Add($"Step {i + 1}: validation threw {exception.Message}");
                }
            }
            return errors;
        }

        internal static bool IsPositiveFinite(float value)
        {
            return value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
