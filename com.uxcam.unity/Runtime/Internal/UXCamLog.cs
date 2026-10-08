using UnityEngine;

namespace UXCamSDK.Internal
{
    internal static class UXCamLog
    {
        const string Prefix = "[UXCam] ";

        internal static bool VerboseEnabled { get; set; }

        internal static void Verbose(string message)
        {
            if (VerboseEnabled) Debug.Log(Prefix + message);
        }

        internal static void Warning(string message) => Debug.LogWarning(Prefix + message);

        internal static void Error(string message) => Debug.LogError(Prefix + message);
    }
}
