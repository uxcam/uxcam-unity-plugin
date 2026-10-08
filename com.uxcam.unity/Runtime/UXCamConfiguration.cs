using System.Collections.Generic;

namespace UXCamSDK
{
    /// <summary>
    /// Options passed to <see cref="UXCam.StartWithConfiguration"/>. Defaults match the native SDKs except where noted.
    /// </summary>
    public sealed class UXCamConfiguration
    {
        public UXCamConfiguration(string appKey)
        {
            AppKey = appKey;
        }

        /// <summary>Your UXCam app key, from the UXCam dashboard.</summary>
        public string AppKey { get; }

        /// <summary>Logs integration diagnostics from the native SDK to Xcode / Logcat. Turn off for release builds.</summary>
        public bool EnableIntegrationLogging { get; set; }

        /// <summary>
        /// Unity only. Opts this device into screen video recording right before UXCam starts. Default <c>false</c>.
        /// </summary>
        /// <remarks>
        /// iOS records no screen video until the device is opted in, and the opt-in persists until the app is
        /// reinstalled. Android records video unless the device opted out. Set this only when you have the user's
        /// consent. Opting in mid-session with <see cref="UXCam.OptIntoVideoRecording"/> restarts the session, so
        /// passing it here avoids an extra session.
        /// </remarks>
        public bool OptIntoVideoRecording { get; set; }

        /// <summary>Records a new session each time the app returns from the background. Default <c>true</c>.</summary>
        public bool EnableMultiSessionRecord { get; set; } = true;

        /// <summary>Captures native crashes. Default <c>true</c>.</summary>
        public bool EnableCrashHandling { get; set; } = true;

        /// <summary>
        /// Native automatic screen tagging. Default <c>false</c>: a Unity game runs in a single native screen
        /// (<c>UnityViewController</c> / <c>UnityPlayerActivity</c>), so native tags would be meaningless.
        /// Use <see cref="TagScenesAutomatically"/> or <see cref="UXCam.TagScreenName"/> instead.
        /// </summary>
        public bool EnableAutomaticScreenNameTagging { get; set; }

        /// <summary>iOS only. Uses the native SDK's improved screen capture pipeline. Default <c>false</c>.</summary>
        public bool EnableImprovedScreenCapture { get; set; }

        /// <summary>iOS only. Recognises swipes, pinches and rotations in addition to taps. Default <c>true</c>.</summary>
        public bool EnableAdvancedGestureRecognition { get; set; } = true;

        /// <summary>iOS only. Records network requests made through <c>NSURLSession</c>. Default <c>false</c>.</summary>
        public bool EnableNetworkLogging { get; set; }

        /// <summary>
        /// Unity only. Tags the active scene's name as the screen name whenever the active scene changes.
        /// Default <c>true</c>.
        /// </summary>
        public bool TagScenesAutomatically { get; set; } = true;

        /// <summary>
        /// Unity only. Reports unhandled C# exceptions (the ones Unity logs with <c>LogType.Exception</c>) as
        /// UXCam exception events. They do not crash the app, so native crash handling never sees them.
        /// Default <c>false</c>.
        /// </summary>
        public bool CaptureUnhandledExceptions { get; set; }

        /// <summary>Occlusions applied from the first frame of every session.</summary>
        public List<UXCamOcclusion> Occlusions { get; } = new List<UXCamOcclusion>();

        internal Dictionary<string, object> ToDictionary()
        {
            var occlusions = new List<object>(Occlusions.Count);
            foreach (var occlusion in Occlusions)
            {
                if (occlusion != null) occlusions.Add(occlusion.ToDictionary());
            }

            return new Dictionary<string, object>
            {
                ["userAppKey"] = AppKey,
                ["enableIntegrationLogging"] = EnableIntegrationLogging,
                ["enableMultiSessionRecord"] = EnableMultiSessionRecord,
                ["enableCrashHandling"] = EnableCrashHandling,
                ["enableAutomaticScreenNameTagging"] = EnableAutomaticScreenNameTagging,
                ["enableImprovedScreenCapture"] = EnableImprovedScreenCapture,
                ["enableAdvancedGestureRecognition"] = EnableAdvancedGestureRecognition,
                ["enableNetworkLogging"] = EnableNetworkLogging,
                ["optIntoVideoRecording"] = OptIntoVideoRecording,
                ["occlusions"] = occlusions,
            };
        }
    }
}
