using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UXCamSDK.Editor
{
    internal sealed class UXCamBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var target = report.summary.platform;
            if (target != BuildTarget.iOS && target != BuildTarget.Android) return;

            var settings = UXCamSettings.Load();
            if (settings == null) return;

            // Not EditorUserBuildSettings.development, which scripted builds don't set.
            bool developmentBuild = (report.summary.options & BuildOptions.Development) != 0;
            foreach (var warning in Validate(settings, target, developmentBuild))
                Debug.LogWarning("[UXCam] " + warning);
        }

        internal static System.Collections.Generic.IEnumerable<string> Validate(UXCamSettings settings, BuildTarget target, bool developmentBuild)
        {
            var platform = target == BuildTarget.iOS ? RuntimePlatform.IPhonePlayer : RuntimePlatform.Android;
            if (settings.StartAutomatically && string.IsNullOrWhiteSpace(settings.ResolveAppKey(platform)))
                yield return $"Start Automatically is on but no app key is set for {target}, so UXCam won't start. Set one in Project Settings > UXCam.";

            if (settings.EnableIntegrationLogging && !developmentBuild)
                yield return "Integration Logging is on in a release build. Turn it off in Project Settings > UXCam before shipping.";
        }
    }
}
