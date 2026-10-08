#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace UXCamSDK.Internal
{
    internal sealed class UXCamPlatformAndroid : IUXCamPlatform
    {
        const string BridgeClass = "com.uxcam.unity.UXCamUnityBridge";

        readonly AndroidJavaClass m_Bridge = new AndroidJavaClass(BridgeClass);

        // JNI maps a null argument to java.lang.Object, which matches no bridge method, so optional strings go as "".
        static string OrEmpty(string value) => value ?? string.Empty;

        sealed class VerificationCallback : AndroidJavaProxy
        {
            readonly Action<bool> m_OnVerified;

            internal VerificationCallback(Action<bool> onVerified)
                : base(BridgeClass + "$VerificationCallback")
            {
                m_OnVerified = onVerified;
            }

            // Called from Java by name.
            [Preserve]
            void onResult(bool verified, string error)
            {
                if (!verified && !string.IsNullOrEmpty(error))
                    UXCamLog.Warning("Verification failed: " + error);
                m_OnVerified?.Invoke(verified);
            }
        }

        static AndroidJavaObject CurrentActivity()
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }

        public void StartWithConfiguration(UXCamConfiguration configuration, Action<bool> onVerified)
        {
            using (var activity = CurrentActivity())
            {
                m_Bridge.CallStatic("startWithConfiguration",
                    activity,
                    MiniJson.Serialize(configuration.ToDictionary()),
                    UXCam.PluginType,
                    UXCam.PluginVersion,
                    new VerificationCallback(onVerified));
            }
        }

        public void StartNewSession() => m_Bridge.CallStatic("startNewSession");
        public void StopSessionAndUploadData() => m_Bridge.CallStatic("stopSessionAndUploadData");
        public void CancelCurrentSession() => m_Bridge.CallStatic("cancelCurrentSession");
        public bool IsRecording() => m_Bridge.CallStatic<bool>("isRecording");
        public void PauseScreenRecording() => m_Bridge.CallStatic("pauseScreenRecording");
        public void ResumeScreenRecording() => m_Bridge.CallStatic("resumeScreenRecording");
        public void AllowShortBreakForAnotherApp(bool continueSession) => m_Bridge.CallStatic("allowShortBreakForAnotherApp", continueSession);

        public void OptInOverall() => m_Bridge.CallStatic("optInOverall");
        public void OptOutOverall() => m_Bridge.CallStatic("optOutOverall");
        public bool OptInOverallStatus() => m_Bridge.CallStatic<bool>("optInOverallStatus");
        public void OptIntoVideoRecording() => m_Bridge.CallStatic("optIntoVideoRecording");
        public void OptOutOfVideoRecording() => m_Bridge.CallStatic("optOutOfVideoRecording");
        public bool OptInVideoRecordingStatus() => m_Bridge.CallStatic<bool>("optInVideoRecordingStatus");

        public void TagScreenName(string screenName, bool isAutomatic) => m_Bridge.CallStatic("tagScreenName", screenName);
        public void AddScreenNameToIgnore(string screenName) => m_Bridge.CallStatic("addScreenNameToIgnore", screenName);
        public void RemoveScreenNameToIgnore(string screenName) => m_Bridge.CallStatic("removeScreenNameToIgnore", screenName);
        public void RemoveAllScreenNamesToIgnore() => m_Bridge.CallStatic("removeAllScreenNamesToIgnore");

        public void SetUserIdentity(string userIdentity) => m_Bridge.CallStatic("setUserIdentity", userIdentity);
        public void SetUserProperty(string key, string jsonValue) => m_Bridge.CallStatic("setUserProperty", key, jsonValue);
        public void SetSessionProperty(string key, string jsonValue) => m_Bridge.CallStatic("setSessionProperty", key, jsonValue);
        public void LogEvent(string eventName, string propertiesJson) => m_Bridge.CallStatic("logEvent", eventName, OrEmpty(propertiesJson));
        public void ReportBugEvent(string eventName, string propertiesJson) => m_Bridge.CallStatic("reportBugEvent", eventName, OrEmpty(propertiesJson));
        public void ReportExceptionEvent(string name, string reason, string framesJson, string propertiesJson) =>
            m_Bridge.CallStatic("reportExceptionEvent", name, OrEmpty(reason), OrEmpty(framesJson), OrEmpty(propertiesJson));
        public void SetPushNotificationToken(string token) => m_Bridge.CallStatic("setPushNotificationToken", token);
        public string UrlForCurrentUser() => m_Bridge.CallStatic<string>("urlForCurrentUser");
        public string UrlForCurrentSession() => m_Bridge.CallStatic<string>("urlForCurrentSession");
        public int PendingUploads() => m_Bridge.CallStatic<int>("pendingUploads");
        public void DeletePendingUploads() => m_Bridge.CallStatic("deletePendingUploads");

        public void OccludeSensitiveScreen(bool hide, bool hideGestures) => m_Bridge.CallStatic("occludeSensitiveScreen", hide, hideGestures);
        public void OccludeAllTextFields(bool occlude) => m_Bridge.CallStatic("occludeAllTextFields", occlude);
        public void ApplyOcclusion(string occlusionJson) => m_Bridge.CallStatic("applyOcclusion", occlusionJson);
        public void RemoveOcclusion(string occlusionJson) => m_Bridge.CallStatic("removeOcclusion", occlusionJson);
        public void SetOcclusionRects(float[] normalizedRects) => m_Bridge.CallStatic("setOcclusionRects", normalizedRects);
    }
}
#endif
