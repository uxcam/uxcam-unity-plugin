using UnityEngine;
using UXCamSDK.Internal;

namespace UXCamSDK
{
    /// <summary>
    /// Project-wide UXCam settings, edited in <b>Project Settings &gt; UXCam</b> and stored in
    /// <c>Assets/Resources/UXCam/UXCamSettings.asset</c> so they are available in player builds.
    /// </summary>
    public sealed class UXCamSettings : ScriptableObject
    {
        internal const string ResourcesPath = "UXCam/UXCamSettings";

        [Tooltip("Your UXCam app key, from the UXCam dashboard.")]
        [SerializeField] string m_AppKey = string.Empty;

        [Tooltip("Optional. Overrides the app key on iOS, for when iOS and Android are separate UXCam apps.")]
        [SerializeField] string m_IOSAppKey = string.Empty;

        [Tooltip("Optional. Overrides the app key on Android, for when iOS and Android are separate UXCam apps.")]
        [SerializeField] string m_AndroidAppKey = string.Empty;

        [Tooltip("Start UXCam automatically after the first scene loads. Turn off to call UXCam.StartWithConfiguration yourself, for example after the user consents.")]
        [SerializeField] bool m_StartAutomatically = true;

        [Tooltip("Opt this device into screen video recording when UXCam starts. iOS records no video without it; Android records video by default. Turn on only if you have the user's consent.")]
        [SerializeField] bool m_OptIntoVideoRecording;

        [Tooltip("Tag the active scene's name as the screen name whenever the active scene changes.")]
        [SerializeField] bool m_TagScenesAutomatically = true;

        [Tooltip("Report unhandled C# exceptions as UXCam exception events.")]
        [SerializeField] bool m_CaptureUnhandledExceptions;

        [Tooltip("Native SDK integration logs in Xcode / Logcat. Turn off for release builds.")]
        [SerializeField] bool m_EnableIntegrationLogging;

        [Tooltip("Record a new session each time the app returns from the background.")]
        [SerializeField] bool m_EnableMultiSessionRecord = true;

        [Tooltip("Capture native crashes.")]
        [SerializeField] bool m_EnableCrashHandling = true;

        [Tooltip("iOS only. Use the native SDK's improved screen capture pipeline.")]
        [SerializeField] bool m_EnableImprovedScreenCapture;

        [Tooltip("iOS only. Recognise swipes, pinches and rotations in addition to taps.")]
        [SerializeField] bool m_EnableAdvancedGestureRecognition = true;

        [Tooltip("iOS only. Record network requests made through NSURLSession.")]
        [SerializeField] bool m_EnableNetworkLogging;

        [Tooltip("Log UXCam calls to the Console in the Editor, where UXCam doesn't record.")]
        [SerializeField] bool m_LogCallsInEditor = true;

        [Tooltip("Outline the rects UXCam would hide in the Game view while in Play Mode.")]
        [SerializeField] bool m_PreviewOcclusionInEditor = true;

        public string AppKey { get => m_AppKey; set => m_AppKey = value; }
        public string IOSAppKey { get => m_IOSAppKey; set => m_IOSAppKey = value; }
        public string AndroidAppKey { get => m_AndroidAppKey; set => m_AndroidAppKey = value; }
        public bool StartAutomatically { get => m_StartAutomatically; set => m_StartAutomatically = value; }
        public bool OptIntoVideoRecording { get => m_OptIntoVideoRecording; set => m_OptIntoVideoRecording = value; }
        public bool TagScenesAutomatically { get => m_TagScenesAutomatically; set => m_TagScenesAutomatically = value; }
        public bool CaptureUnhandledExceptions { get => m_CaptureUnhandledExceptions; set => m_CaptureUnhandledExceptions = value; }
        public bool EnableIntegrationLogging { get => m_EnableIntegrationLogging; set => m_EnableIntegrationLogging = value; }
        public bool EnableMultiSessionRecord { get => m_EnableMultiSessionRecord; set => m_EnableMultiSessionRecord = value; }
        public bool EnableCrashHandling { get => m_EnableCrashHandling; set => m_EnableCrashHandling = value; }
        public bool EnableImprovedScreenCapture { get => m_EnableImprovedScreenCapture; set => m_EnableImprovedScreenCapture = value; }
        public bool EnableAdvancedGestureRecognition { get => m_EnableAdvancedGestureRecognition; set => m_EnableAdvancedGestureRecognition = value; }
        public bool EnableNetworkLogging { get => m_EnableNetworkLogging; set => m_EnableNetworkLogging = value; }
        public bool LogCallsInEditor { get => m_LogCallsInEditor; set => m_LogCallsInEditor = value; }
        public bool PreviewOcclusionInEditor { get => m_PreviewOcclusionInEditor; set => m_PreviewOcclusionInEditor = value; }

        /// <summary>The app key for the current platform: the platform override if set, otherwise <see cref="AppKey"/>.</summary>
        public string ResolveAppKey(RuntimePlatform platform)
        {
            string overrideKey = platform == RuntimePlatform.IPhonePlayer ? m_IOSAppKey
                : platform == RuntimePlatform.Android ? m_AndroidAppKey
                : null;
            return string.IsNullOrWhiteSpace(overrideKey) ? m_AppKey?.Trim() : overrideKey.Trim();
        }

        /// <summary>Builds a configuration from these settings for the given platform.</summary>
        public UXCamConfiguration ToConfiguration(RuntimePlatform platform)
        {
            return new UXCamConfiguration(ResolveAppKey(platform))
            {
                OptIntoVideoRecording = m_OptIntoVideoRecording,
                TagScenesAutomatically = m_TagScenesAutomatically,
                CaptureUnhandledExceptions = m_CaptureUnhandledExceptions,
                EnableIntegrationLogging = m_EnableIntegrationLogging,
                EnableMultiSessionRecord = m_EnableMultiSessionRecord,
                EnableCrashHandling = m_EnableCrashHandling,
                EnableImprovedScreenCapture = m_EnableImprovedScreenCapture,
                EnableAdvancedGestureRecognition = m_EnableAdvancedGestureRecognition,
                EnableNetworkLogging = m_EnableNetworkLogging,
            };
        }

        /// <summary>Loads the settings asset from Resources, or <c>null</c> if the project has none.</summary>
        public static UXCamSettings Load() => Resources.Load<UXCamSettings>(ResourcesPath);

#if !UXCAM_DISABLE_AUTO_INIT
        // AfterSceneLoad so the first scene is active and can be tagged as the first screen.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
#endif
        static void AutoStart()
        {
            var settings = Load();
            if (settings == null) return;

            UXCamLog.VerboseEnabled = Application.isEditor && settings.m_LogCallsInEditor;
            if (!settings.m_StartAutomatically) return;

            var configuration = settings.ToConfiguration(Application.platform);
            if (string.IsNullOrWhiteSpace(configuration.AppKey))
            {
                UXCamLog.Warning("Start Automatically is on but no app key is set. Add one in Project Settings > UXCam.");
                return;
            }

            UXCam.StartWithConfiguration(configuration);
        }
    }
}
