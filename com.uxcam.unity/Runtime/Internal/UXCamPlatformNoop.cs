using System;

namespace UXCamSDK.Internal
{
    internal sealed class UXCamPlatformNoop : IUXCamPlatform
    {
        bool m_OptedIn = true;
        bool m_VideoOptedIn = true;

        static void Log(string call) => UXCamLog.Verbose(call + " (no-op: UXCam only records on iOS and Android devices)");

        public void StartWithConfiguration(UXCamConfiguration configuration, Action<bool> onVerified)
        {
            Log($"StartWithConfiguration(appKey: {Mask(configuration.AppKey)})");
            onVerified?.Invoke(false);
        }

        public void StartNewSession() => Log(nameof(StartNewSession));
        public void StopSessionAndUploadData() => Log(nameof(StopSessionAndUploadData));
        public void CancelCurrentSession() => Log(nameof(CancelCurrentSession));
        public bool IsRecording() => false;
        public void PauseScreenRecording() => Log(nameof(PauseScreenRecording));
        public void ResumeScreenRecording() => Log(nameof(ResumeScreenRecording));
        public void AllowShortBreakForAnotherApp(bool continueSession) => Log($"AllowShortBreakForAnotherApp({continueSession})");

        public void OptInOverall() { m_OptedIn = true; Log(nameof(OptInOverall)); }
        public void OptOutOverall() { m_OptedIn = false; Log(nameof(OptOutOverall)); }
        public bool OptInOverallStatus() => m_OptedIn;
        public void OptIntoVideoRecording() { m_VideoOptedIn = true; Log(nameof(OptIntoVideoRecording)); }
        public void OptOutOfVideoRecording() { m_VideoOptedIn = false; Log(nameof(OptOutOfVideoRecording)); }
        public bool OptInVideoRecordingStatus() => m_VideoOptedIn;

        public void TagScreenName(string screenName, bool isAutomatic) => Log($"TagScreenName(\"{screenName}\"{(isAutomatic ? ", scene" : "")})");
        public void AddScreenNameToIgnore(string screenName) => Log($"AddScreenNameToIgnore(\"{screenName}\")");
        public void RemoveScreenNameToIgnore(string screenName) => Log($"RemoveScreenNameToIgnore(\"{screenName}\")");
        public void RemoveAllScreenNamesToIgnore() => Log(nameof(RemoveAllScreenNamesToIgnore));

        public void SetUserIdentity(string userIdentity) => Log($"SetUserIdentity(\"{userIdentity}\")");
        public void SetUserProperty(string key, string jsonValue) => Log($"SetUserProperty(\"{key}\", {jsonValue})");
        public void SetSessionProperty(string key, string jsonValue) => Log($"SetSessionProperty(\"{key}\", {jsonValue})");
        public void LogEvent(string eventName, string propertiesJson) => Log($"LogEvent(\"{eventName}\", {propertiesJson ?? "null"})");
        public void ReportBugEvent(string eventName, string propertiesJson) => Log($"ReportBugEvent(\"{eventName}\", {propertiesJson ?? "null"})");
        public void ReportExceptionEvent(string name, string reason, string framesJson, string propertiesJson) => Log($"ReportExceptionEvent({name}: {reason})");
        public void SetPushNotificationToken(string token) => Log(nameof(SetPushNotificationToken));
        public string UrlForCurrentUser() => null;
        public string UrlForCurrentSession() => null;
        public int PendingUploads() => 0;
        public void DeletePendingUploads() => Log(nameof(DeletePendingUploads));

        public void OccludeSensitiveScreen(bool hide, bool hideGestures) => Log($"OccludeSensitiveScreen({hide}, {hideGestures})");
        public void OccludeAllTextFields(bool occlude) => Log($"OccludeAllTextFields({occlude})");
        public void ApplyOcclusion(string occlusionJson) => Log($"ApplyOcclusion({occlusionJson})");
        public void RemoveOcclusion(string occlusionJson) => Log($"RemoveOcclusion({occlusionJson})");
        // Not logged: rects change every frame. UXCamRuntime draws them in the Game view instead.
        public void SetOcclusionRects(float[] normalizedRects) => LastOcclusionRects = normalizedRects;

        internal float[] LastOcclusionRects { get; private set; } = new float[0];

        static string Mask(string key) => string.IsNullOrEmpty(key) || key.Length <= 4 ? "****" : key.Substring(0, 4) + "****";
    }
}
