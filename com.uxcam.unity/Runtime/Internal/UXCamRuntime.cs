using System.Collections;
using UnityEngine;

namespace UXCamSDK.Internal
{
    [AddComponentMenu("")]
    internal sealed class UXCamRuntime : MonoBehaviour
    {
        static UXCamRuntime s_Instance;
        static readonly WaitForEndOfFrame s_EndOfFrame = new WaitForEndOfFrame();

        internal static void EnsureCreated()
        {
            if (s_Instance != null || !Application.isPlaying) return;

            // Not HideAndDontSave: in the Editor that would outlive Play Mode.
            var go = new GameObject("UXCam");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<UXCamRuntime>();
        }

        void OnEnable()
        {
            StartCoroutine(FlushOcclusionAtEndOfFrame());
        }

        void Update()
        {
            UXCamMainThread.Drain();
        }

        // End of frame: layout and rendering are done, so UI rects reflect what is on screen this frame.
        IEnumerator FlushOcclusionAtEndOfFrame()
        {
            while (true)
            {
                yield return s_EndOfFrame;
                OcclusionRectTracker.Flush(UXCam.Platform, Screen.width, Screen.height);
            }
        }

        void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
#if UNITY_EDITOR
            if (m_PreviewTexture != null) Destroy(m_PreviewTexture);
#endif
        }

#if UNITY_EDITOR
        bool m_PreviewOcclusion;
        Texture2D m_PreviewTexture;

        void Awake()
        {
            var settings = UXCamSettings.Load();
            m_PreviewOcclusion = settings == null || settings.PreviewOcclusionInEditor;
        }

        void OnGUI()
        {
            if (!m_PreviewOcclusion || Event.current.type != EventType.Repaint) return;
            if (!(UXCam.Platform is UXCamPlatformNoop noop)) return;

            var rects = noop.LastOcclusionRects;
            if (rects.Length == 0) return;

            if (m_PreviewTexture == null)
            {
                m_PreviewTexture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                m_PreviewTexture.SetPixel(0, 0, new Color(1f, 0.1f, 0.1f, 0.45f));
                m_PreviewTexture.Apply();
            }

            // Normalised top-left rects map directly onto GUI coordinates.
            for (int i = 0; i + 3 < rects.Length; i += 4)
            {
                var r = new Rect(rects[i] * Screen.width, rects[i + 1] * Screen.height,
                    rects[i + 2] * Screen.width, rects[i + 3] * Screen.height);
                GUI.DrawTexture(r, m_PreviewTexture);
            }
        }
#endif
    }
}
