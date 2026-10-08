using UnityEngine;

namespace UXCamSDK.Internal
{
    internal static class UXCamExceptionReporter
    {
        const float DuplicateWindowSeconds = 5f;

        static bool s_Enabled;
        static bool s_Reporting;
        static string s_LastCondition;
        static float s_LastReportTime = float.NegativeInfinity;

        internal static void Enable()
        {
            if (s_Enabled) return;
            Application.logMessageReceived += OnLogMessageReceived;
            s_Enabled = true;
        }

        internal static void Disable()
        {
            if (!s_Enabled) return;
            Application.logMessageReceived -= OnLogMessageReceived;
            s_Enabled = false;
        }

        static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception) return;

            float now = Time.realtimeSinceStartup;
            if (condition == s_LastCondition && now - s_LastReportTime < DuplicateWindowSeconds) return;
            s_LastCondition = condition;
            s_LastReportTime = now;

            // condition is "ExceptionType: message".
            string name = "Exception", reason = condition ?? string.Empty;
            int colon = reason.IndexOf(": ", System.StringComparison.Ordinal);
            if (colon > 0)
            {
                name = reason.Substring(0, colon);
                reason = reason.Substring(colon + 2);
            }

            // Never let a reporting failure surface as another logged exception, which would be reported in turn.
            if (s_Reporting) return;
            s_Reporting = true;
            try
            {
                UXCam.ReportException(name, reason, stackTrace, null);
            }
            catch (System.Exception e)
            {
                UXCamLog.Warning("Could not report exception: " + e.Message);
            }
            finally
            {
                s_Reporting = false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Disable();
            s_LastCondition = null;
            s_LastReportTime = float.NegativeInfinity;
        }
    }
}
