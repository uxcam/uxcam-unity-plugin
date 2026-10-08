using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UXCamSDK.Internal;

namespace UXCamSDK
{
    /// <summary>
    /// Entry point for UXCam on iOS and Android.
    /// </summary>
    /// <remarks>
    /// Every method is safe to call in the Unity Editor and on unsupported platforms, where it does nothing
    /// and returns a default value. Call the API from the Unity main thread. Callbacks are always delivered on
    /// the Unity main thread.
    /// </remarks>
    public static class UXCam
    {
        /// <summary>Version of this Unity package.</summary>
        public const string PluginVersion = "1.0.0";

        internal const string PluginType = "unity";

        static IUXCamPlatform s_Platform;
        static bool s_SceneTaggingHooked;

        internal static IUXCamPlatform Platform => s_Platform ?? (s_Platform = UXCamPlatformFactory.Create());

        /// <summary>The configuration passed to the most recent <see cref="StartWithConfiguration"/> call.</summary>
        public static UXCamConfiguration Configuration { get; private set; }

        #region Session

        /// <summary>
        /// Starts UXCam. Call once, as early as possible. If a <c>UXCamSettings</c> asset has
        /// <c>Start Automatically</c> enabled, the plugin calls this for you.
        /// </summary>
        /// <param name="configuration">The configuration. <see cref="UXCamConfiguration.AppKey"/> is required.</param>
        /// <param name="onVerified">
        /// Optional. Invoked once on the main thread with <c>true</c> when the app key was verified and recording
        /// started, or <c>false</c> when verification failed.
        /// </param>
        public static void StartWithConfiguration(UXCamConfiguration configuration, Action<bool> onVerified = null)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrWhiteSpace(configuration.AppKey))
            {
                UXCamLog.Error("StartWithConfiguration: AppKey is empty. Find your key at https://app.uxcam.com.");
                onVerified?.Invoke(false);
                return;
            }

            Configuration = configuration;
            UXCamRuntime.EnsureCreated();

            if (configuration.CaptureUnhandledExceptions)
                UXCamExceptionReporter.Enable();
            else
                UXCamExceptionReporter.Disable();

            Platform.StartWithConfiguration(configuration, onVerified == null
                ? (Action<bool>)null
                : verified => UXCamMainThread.Post(() => onVerified(verified)));

            if (configuration.TagScenesAutomatically)
                HookSceneTagging();
        }

        /// <summary>Starts a new session, ending the current one.</summary>
        public static void StartNewSession() => Platform.StartNewSession();

        /// <summary>Stops the current session and uploads its data. A new session starts when the app returns from the background.</summary>
        public static void StopSessionAndUploadData() => Platform.StopSessionAndUploadData();

        /// <summary>Cancels the current session and discards its data.</summary>
        public static void CancelCurrentSession() => Platform.CancelCurrentSession();

        /// <summary>Whether a session is currently being recorded.</summary>
        public static bool IsRecording() => Platform.IsRecording();

        /// <summary>Pauses screen recording. Events are still collected.</summary>
        public static void PauseScreenRecording() => Platform.PauseScreenRecording();

        /// <summary>Resumes screen recording after <see cref="PauseScreenRecording"/>.</summary>
        public static void ResumeScreenRecording() => Platform.ResumeScreenRecording();

        /// <summary>
        /// Keeps the session alive while the user briefly leaves the app, for example to share or to pick a photo.
        /// Call it right before presenting the other app.
        /// </summary>
        /// <param name="continueSession"><c>true</c> to continue the current session when the user comes back.</param>
        public static void AllowShortBreakForAnotherApp(bool continueSession) => Platform.AllowShortBreakForAnotherApp(continueSession);

        #endregion

        #region Opt in / out

        /// <summary>Opts the user back into UXCam data collection.</summary>
        public static void OptInOverall() => Platform.OptInOverall();

        /// <summary>Opts the user out of all UXCam data collection. Persists across launches.</summary>
        public static void OptOutOverall() => Platform.OptOutOverall();

        /// <summary>Whether the user is opted into data collection.</summary>
        public static bool OptInOverallStatus() => Platform.OptInOverallStatus();

        /// <summary>
        /// Opts the user into screen video recording. iOS records no video until this is called (Android records
        /// video by default). Calling it during a session restarts the session; to opt in at launch, set
        /// <see cref="UXCamConfiguration.OptIntoVideoRecording"/> instead.
        /// </summary>
        public static void OptIntoVideoRecording() => Platform.OptIntoVideoRecording();

        /// <summary>Opts the user out of screen video recording; events and analytics are still collected.</summary>
        public static void OptOutOfVideoRecording() => Platform.OptOutOfVideoRecording();

        /// <summary>Whether the user is opted into screen video recording.</summary>
        public static bool OptInVideoRecordingStatus() => Platform.OptInVideoRecordingStatus();

        #endregion

        #region Screens

        /// <summary>
        /// Tags the current screen. By default the plugin tags each active Unity scene by name; call this for
        /// finer-grained screens such as menus or dialogs within a scene.
        /// </summary>
        public static void TagScreenName(string screenName)
        {
            if (!RequireNonEmpty(screenName, nameof(TagScreenName))) return;
            Platform.TagScreenName(screenName, isAutomatic: false);
        }

        /// <summary>Stops recording video while the named screen is visible.</summary>
        public static void AddScreenNameToIgnore(string screenName)
        {
            if (!RequireNonEmpty(screenName, nameof(AddScreenNameToIgnore))) return;
            Platform.AddScreenNameToIgnore(screenName);
        }

        /// <summary>Removes a screen added with <see cref="AddScreenNameToIgnore"/>.</summary>
        public static void RemoveScreenNameToIgnore(string screenName)
        {
            if (!RequireNonEmpty(screenName, nameof(RemoveScreenNameToIgnore))) return;
            Platform.RemoveScreenNameToIgnore(screenName);
        }

        /// <summary>Removes every screen added with <see cref="AddScreenNameToIgnore"/>.</summary>
        public static void RemoveAllScreenNamesToIgnore() => Platform.RemoveAllScreenNamesToIgnore();

        #endregion

        #region Users, sessions, events

        /// <summary>Sets an identifier for the current user, such as your own user ID.</summary>
        public static void SetUserIdentity(string userIdentity)
        {
            if (!RequireNonEmpty(userIdentity, nameof(SetUserIdentity))) return;
            Platform.SetUserIdentity(userIdentity);
        }

        /// <summary>Sets a property on the current user.</summary>
        public static void SetUserProperty(string key, string value) => SetUserPropertyCore(key, value);

        /// <inheritdoc cref="SetUserProperty(string,string)"/>
        public static void SetUserProperty(string key, int value) => SetUserPropertyCore(key, value);

        /// <inheritdoc cref="SetUserProperty(string,string)"/>
        public static void SetUserProperty(string key, float value) => SetUserPropertyCore(key, value);

        /// <inheritdoc cref="SetUserProperty(string,string)"/>
        public static void SetUserProperty(string key, bool value) => SetUserPropertyCore(key, value);

        /// <summary>Sets a property on the current session.</summary>
        public static void SetSessionProperty(string key, string value) => SetSessionPropertyCore(key, value);

        /// <inheritdoc cref="SetSessionProperty(string,string)"/>
        public static void SetSessionProperty(string key, int value) => SetSessionPropertyCore(key, value);

        /// <inheritdoc cref="SetSessionProperty(string,string)"/>
        public static void SetSessionProperty(string key, float value) => SetSessionPropertyCore(key, value);

        /// <inheritdoc cref="SetSessionProperty(string,string)"/>
        public static void SetSessionProperty(string key, bool value) => SetSessionPropertyCore(key, value);

        /// <summary>Logs a custom event.</summary>
        /// <param name="eventName">The event name.</param>
        /// <param name="properties">
        /// Optional properties. Values may be strings, numbers, booleans, or nested dictionaries and lists of those.
        /// </param>
        public static void LogEvent(string eventName, IDictionary<string, object> properties = null)
        {
            if (!RequireNonEmpty(eventName, nameof(LogEvent))) return;
            Platform.LogEvent(eventName, properties == null || properties.Count == 0 ? null : MiniJson.Serialize(properties));
        }

        /// <summary>Reports a bug event, shown in the session timeline and in the Issues dashboard.</summary>
        public static void ReportBugEvent(string eventName, IDictionary<string, object> properties = null)
        {
            if (!RequireNonEmpty(eventName, nameof(ReportBugEvent))) return;
            Platform.ReportBugEvent(eventName, properties == null || properties.Count == 0 ? null : MiniJson.Serialize(properties));
        }

        /// <summary>Reports a handled C# exception, including its stack trace.</summary>
        public static void ReportExceptionEvent(Exception exception, IDictionary<string, object> properties = null)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            ReportException(exception.GetType().FullName, exception.Message, exception.StackTrace, properties);
        }

        internal static void ReportException(string name, string reason, string stackTrace, IDictionary<string, object> properties)
        {
            var frames = StackTraceParser.Parse(stackTrace);
            Platform.ReportExceptionEvent(
                string.IsNullOrEmpty(name) ? "Exception" : name,
                reason ?? string.Empty,
                MiniJson.Serialize(frames),
                properties == null || properties.Count == 0 ? null : MiniJson.Serialize(properties));
        }

        /// <summary>Registers the device push token so UXCam can attribute sessions opened from notifications.</summary>
        public static void SetPushNotificationToken(string token)
        {
            if (!RequireNonEmpty(token, nameof(SetPushNotificationToken))) return;
            Platform.SetPushNotificationToken(token);
        }

        /// <summary>Dashboard URL for the current user, or <c>null</c> before the session is verified.</summary>
        public static string UrlForCurrentUser() => Platform.UrlForCurrentUser();

        /// <summary>Dashboard URL for the current session, or <c>null</c> before the session is verified.</summary>
        public static string UrlForCurrentSession() => Platform.UrlForCurrentSession();

        /// <summary>Number of sessions recorded but not yet uploaded.</summary>
        public static int PendingUploads() => Platform.PendingUploads();

        /// <summary>Deletes sessions recorded but not yet uploaded.</summary>
        public static void DeletePendingUploads() => Platform.DeletePendingUploads();

        #endregion

        #region Occlusion

        /// <summary>
        /// Hides the whole screen in the recording until called again with <c>false</c>. Use it for screens
        /// that show sensitive data, such as payment or account details.
        /// </summary>
        /// <param name="hide"><c>true</c> to hide the screen.</param>
        /// <param name="hideGestures"><c>true</c> to also stop recording touches while hidden.</param>
        public static void OccludeSensitiveScreen(bool hide, bool hideGestures = true) => Platform.OccludeSensitiveScreen(hide, hideGestures);

        /// <summary>
        /// Hides native text fields, such as the input box iOS and Android show over the keyboard for
        /// <see cref="TouchScreenKeyboard"/>. Unity UI text is not native; use <see cref="UXCamOcclude"/> for it.
        /// </summary>
        public static void OccludeAllTextFields(bool occlude) => Platform.OccludeAllTextFields(occlude);

        /// <summary>Applies a blur or overlay occlusion at runtime.</summary>
        /// <remarks>
        /// On iOS, <see cref="UXCamOcclusion.ExcludeMentionedScreens"/> is only honoured when the occlusion is passed in
        /// <see cref="UXCamConfiguration.Occlusions"/>; at runtime the occlusion applies to <see cref="UXCamOcclusion.Screens"/>.
        /// </remarks>
        public static void ApplyOcclusion(UXCamOcclusion occlusion)
        {
            if (occlusion == null) throw new ArgumentNullException(nameof(occlusion));
            Platform.ApplyOcclusion(MiniJson.Serialize(occlusion.ToDictionary()));
        }

        /// <summary>Removes an occlusion applied with <see cref="ApplyOcclusion"/>. Occlusions are matched by type.</summary>
        public static void RemoveOcclusion(UXCamOcclusion occlusion)
        {
            if (occlusion == null) throw new ArgumentNullException(nameof(occlusion));
            Platform.RemoveOcclusion(MiniJson.Serialize(occlusion.ToDictionary()));
        }

        /// <summary>
        /// Hides screen-space rectangles in the recording, for content that has no <see cref="UXCamOcclude"/>
        /// component, such as an <c>OnGUI</c> panel. Rects persist until replaced or cleared.
        /// </summary>
        /// <param name="id">A key that identifies this set of rects, so several systems can occlude independently.</param>
        /// <param name="screenRects">Rects in Unity screen pixels (origin bottom-left, like <see cref="Input.mousePosition"/>).</param>
        public static void SetOcclusionRects(string id, IEnumerable<Rect> screenRects)
        {
            if (!RequireNonEmpty(id, nameof(SetOcclusionRects))) return;
            UXCamRuntime.EnsureCreated();
            OcclusionRectTracker.SetManualRects(id, screenRects);
        }

        /// <summary>Clears rects set with <see cref="SetOcclusionRects"/> for the given id.</summary>
        public static void ClearOcclusionRects(string id)
        {
            if (!RequireNonEmpty(id, nameof(ClearOcclusionRects))) return;
            OcclusionRectTracker.SetManualRects(id, null);
        }

        #endregion

        static void HookSceneTagging()
        {
            if (!s_SceneTaggingHooked)
            {
                SceneManager.activeSceneChanged += OnActiveSceneChanged;
                s_SceneTaggingHooked = true;
            }

            var active = SceneManager.GetActiveScene();
            if (active.IsValid() && !string.IsNullOrEmpty(active.name))
                Platform.TagScreenName(active.name, isAutomatic: true);
        }

        static void OnActiveSceneChanged(Scene previous, Scene next)
        {
            if (Configuration == null || !Configuration.TagScenesAutomatically) return;
            if (!string.IsNullOrEmpty(next.name))
                Platform.TagScreenName(next.name, isAutomatic: true);
        }

        static void SetUserPropertyCore(string key, object value)
        {
            if (!RequireNonEmpty(key, nameof(SetUserProperty))) return;
            Platform.SetUserProperty(key, MiniJson.Serialize(value));
        }

        static void SetSessionPropertyCore(string key, object value)
        {
            if (!RequireNonEmpty(key, nameof(SetSessionProperty))) return;
            Platform.SetSessionProperty(key, MiniJson.Serialize(value));
        }

        static bool RequireNonEmpty(string value, string caller)
        {
            if (!string.IsNullOrWhiteSpace(value)) return true;
            UXCamLog.Warning($"{caller}: argument must not be empty; call ignored.");
            return false;
        }

        // Domain reload may be disabled in the Editor ("Enter Play Mode Options"), so static state must be reset.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            if (s_SceneTaggingHooked) SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            s_SceneTaggingHooked = false;
            s_Platform = null;
            Configuration = null;
        }
    }
}
