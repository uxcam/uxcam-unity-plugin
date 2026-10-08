#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using AOT;

namespace UXCamSDK.Internal
{
    internal sealed class UXCamPlatformIOS : IUXCamPlatform
    {
        // int rather than bool: a C bool passed through a function pointer has no portable marshalling.
        delegate void VerificationCallback(int requestId, int verified);

        static readonly object s_Lock = new object();
        static readonly System.Collections.Generic.Dictionary<int, Action<bool>> s_Pending =
            new System.Collections.Generic.Dictionary<int, Action<bool>>();
        static int s_NextRequestId;

        public void StartWithConfiguration(UXCamConfiguration configuration, Action<bool> onVerified)
        {
            int requestId = 0;
            if (onVerified != null)
            {
                lock (s_Lock)
                {
                    requestId = ++s_NextRequestId;
                    s_Pending[requestId] = onVerified;
                }
            }

            _UXCam_StartWithConfiguration(
                MiniJson.Serialize(configuration.ToDictionary()),
                UXCam.PluginType,
                UXCam.PluginVersion,
                requestId,
                OnVerified);
        }

        // IL2CPP only calls back into static methods marked MonoPInvokeCallback.
        [MonoPInvokeCallback(typeof(VerificationCallback))]
        static void OnVerified(int requestId, int verified)
        {
            Action<bool> callback;
            lock (s_Lock)
            {
                if (!s_Pending.TryGetValue(requestId, out callback)) return;
                s_Pending.Remove(requestId);
            }
            callback(verified != 0);
        }

        public void StartNewSession() => _UXCam_StartNewSession();
        public void StopSessionAndUploadData() => _UXCam_StopSessionAndUploadData();
        public void CancelCurrentSession() => _UXCam_CancelCurrentSession();
        public bool IsRecording() => _UXCam_IsRecording();
        public void PauseScreenRecording() => _UXCam_PauseScreenRecording();
        public void ResumeScreenRecording() => _UXCam_ResumeScreenRecording();
        public void AllowShortBreakForAnotherApp(bool continueSession) => _UXCam_AllowShortBreakForAnotherApp(continueSession);

        public void OptInOverall() => _UXCam_OptInOverall();
        public void OptOutOverall() => _UXCam_OptOutOverall();
        public bool OptInOverallStatus() => _UXCam_OptInOverallStatus();
        public void OptIntoVideoRecording() => _UXCam_OptIntoVideoRecording();
        public void OptOutOfVideoRecording() => _UXCam_OptOutOfVideoRecording();
        public bool OptInVideoRecordingStatus() => _UXCam_OptInVideoRecordingStatus();

        public void TagScreenName(string screenName, bool isAutomatic) => _UXCam_TagScreenName(screenName, isAutomatic);
        public void AddScreenNameToIgnore(string screenName) => _UXCam_AddScreenNameToIgnore(screenName);
        public void RemoveScreenNameToIgnore(string screenName) => _UXCam_RemoveScreenNameToIgnore(screenName);
        public void RemoveAllScreenNamesToIgnore() => _UXCam_RemoveAllScreenNamesToIgnore();

        public void SetUserIdentity(string userIdentity) => _UXCam_SetUserIdentity(userIdentity);
        public void SetUserProperty(string key, string jsonValue) => _UXCam_SetUserProperty(key, jsonValue);
        public void SetSessionProperty(string key, string jsonValue) => _UXCam_SetSessionProperty(key, jsonValue);
        public void LogEvent(string eventName, string propertiesJson) => _UXCam_LogEvent(eventName, propertiesJson);
        public void ReportBugEvent(string eventName, string propertiesJson) => _UXCam_ReportBugEvent(eventName, propertiesJson);
        public void ReportExceptionEvent(string name, string reason, string framesJson, string propertiesJson) =>
            _UXCam_ReportExceptionEvent(name, reason, framesJson, propertiesJson);
        public void SetPushNotificationToken(string token) => _UXCam_SetPushNotificationToken(token);
        public string UrlForCurrentUser() => _UXCam_UrlForCurrentUser();
        public string UrlForCurrentSession() => _UXCam_UrlForCurrentSession();
        public int PendingUploads() => _UXCam_PendingUploads();
        public void DeletePendingUploads() => _UXCam_DeletePendingUploads();

        public void OccludeSensitiveScreen(bool hide, bool hideGestures) => _UXCam_OccludeSensitiveScreen(hide, hideGestures);
        public void OccludeAllTextFields(bool occlude) => _UXCam_OccludeAllTextFields(occlude);
        public void ApplyOcclusion(string occlusionJson) => _UXCam_ApplyOcclusion(occlusionJson);
        public void RemoveOcclusion(string occlusionJson) => _UXCam_RemoveOcclusion(occlusionJson);
        public void SetOcclusionRects(float[] normalizedRects) => _UXCam_SetOcclusionRects(normalizedRects, normalizedRects.Length / 4);

        // Native strings returned to C# are malloc'd copies; the marshaller frees them.
        [DllImport("__Internal")] static extern void _UXCam_StartWithConfiguration(string configJson, string pluginType, string pluginVersion, int requestId, VerificationCallback callback);
        [DllImport("__Internal")] static extern void _UXCam_StartNewSession();
        [DllImport("__Internal")] static extern void _UXCam_StopSessionAndUploadData();
        [DllImport("__Internal")] static extern void _UXCam_CancelCurrentSession();
        [DllImport("__Internal")] [return: MarshalAs(UnmanagedType.I1)] static extern bool _UXCam_IsRecording();
        [DllImport("__Internal")] static extern void _UXCam_PauseScreenRecording();
        [DllImport("__Internal")] static extern void _UXCam_ResumeScreenRecording();
        [DllImport("__Internal")] static extern void _UXCam_AllowShortBreakForAnotherApp([MarshalAs(UnmanagedType.I1)] bool continueSession);

        [DllImport("__Internal")] static extern void _UXCam_OptInOverall();
        [DllImport("__Internal")] static extern void _UXCam_OptOutOverall();
        [DllImport("__Internal")] [return: MarshalAs(UnmanagedType.I1)] static extern bool _UXCam_OptInOverallStatus();
        [DllImport("__Internal")] static extern void _UXCam_OptIntoVideoRecording();
        [DllImport("__Internal")] static extern void _UXCam_OptOutOfVideoRecording();
        [DllImport("__Internal")] [return: MarshalAs(UnmanagedType.I1)] static extern bool _UXCam_OptInVideoRecordingStatus();

        [DllImport("__Internal")] static extern void _UXCam_TagScreenName(string screenName, [MarshalAs(UnmanagedType.I1)] bool isAutomatic);
        [DllImport("__Internal")] static extern void _UXCam_AddScreenNameToIgnore(string screenName);
        [DllImport("__Internal")] static extern void _UXCam_RemoveScreenNameToIgnore(string screenName);
        [DllImport("__Internal")] static extern void _UXCam_RemoveAllScreenNamesToIgnore();

        [DllImport("__Internal")] static extern void _UXCam_SetUserIdentity(string userIdentity);
        [DllImport("__Internal")] static extern void _UXCam_SetUserProperty(string key, string jsonValue);
        [DllImport("__Internal")] static extern void _UXCam_SetSessionProperty(string key, string jsonValue);
        [DllImport("__Internal")] static extern void _UXCam_LogEvent(string eventName, string propertiesJson);
        [DllImport("__Internal")] static extern void _UXCam_ReportBugEvent(string eventName, string propertiesJson);
        [DllImport("__Internal")] static extern void _UXCam_ReportExceptionEvent(string name, string reason, string framesJson, string propertiesJson);
        [DllImport("__Internal")] static extern void _UXCam_SetPushNotificationToken(string token);
        [DllImport("__Internal")] static extern string _UXCam_UrlForCurrentUser();
        [DllImport("__Internal")] static extern string _UXCam_UrlForCurrentSession();
        [DllImport("__Internal")] static extern int _UXCam_PendingUploads();
        [DllImport("__Internal")] static extern void _UXCam_DeletePendingUploads();

        [DllImport("__Internal")] static extern void _UXCam_OccludeSensitiveScreen([MarshalAs(UnmanagedType.I1)] bool hide, [MarshalAs(UnmanagedType.I1)] bool hideGestures);
        [DllImport("__Internal")] static extern void _UXCam_OccludeAllTextFields([MarshalAs(UnmanagedType.I1)] bool occlude);
        [DllImport("__Internal")] static extern void _UXCam_ApplyOcclusion(string occlusionJson);
        [DllImport("__Internal")] static extern void _UXCam_RemoveOcclusion(string occlusionJson);
        [DllImport("__Internal")] static extern void _UXCam_SetOcclusionRects(float[] normalizedRects, int count);
    }
}
#endif
