using System;

namespace UXCamSDK.Internal
{
    internal interface IUXCamPlatform
    {
        void StartWithConfiguration(UXCamConfiguration configuration, Action<bool> onVerified);
        void StartNewSession();
        void StopSessionAndUploadData();
        void CancelCurrentSession();
        bool IsRecording();
        void PauseScreenRecording();
        void ResumeScreenRecording();
        void AllowShortBreakForAnotherApp(bool continueSession);

        void OptInOverall();
        void OptOutOverall();
        bool OptInOverallStatus();
        void OptIntoVideoRecording();
        void OptOutOfVideoRecording();
        bool OptInVideoRecordingStatus();

        void TagScreenName(string screenName, bool isAutomatic);
        void AddScreenNameToIgnore(string screenName);
        void RemoveScreenNameToIgnore(string screenName);
        void RemoveAllScreenNamesToIgnore();

        void SetUserIdentity(string userIdentity);
        void SetUserProperty(string key, string jsonValue);
        void SetSessionProperty(string key, string jsonValue);
        void LogEvent(string eventName, string propertiesJson);
        void ReportBugEvent(string eventName, string propertiesJson);
        void ReportExceptionEvent(string name, string reason, string framesJson, string propertiesJson);
        void SetPushNotificationToken(string token);
        string UrlForCurrentUser();
        string UrlForCurrentSession();
        int PendingUploads();
        void DeletePendingUploads();

        void OccludeSensitiveScreen(bool hide, bool hideGestures);
        void OccludeAllTextFields(bool occlude);
        void ApplyOcclusion(string occlusionJson);
        void RemoveOcclusion(string occlusionJson);

        // x, y, width, height quadruples, normalised to the Unity surface with the origin top-left. Empty clears all.
        void SetOcclusionRects(float[] normalizedRects);
    }

    internal static class UXCamPlatformFactory
    {
        internal static IUXCamPlatform Create()
        {
            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                return new UXCamPlatformIOS();
#elif UNITY_ANDROID && !UNITY_EDITOR
                return new UXCamPlatformAndroid();
#else
                return new UXCamPlatformNoop();
#endif
            }
            catch (Exception e)
            {
                UXCamLog.Error("Could not load the native UXCam bridge; UXCam is disabled. " + e.Message);
                return new UXCamPlatformNoop();
            }
        }
    }
}
