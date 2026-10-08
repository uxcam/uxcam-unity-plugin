using UnityEngine;
using UXCamSDK.Internal;

namespace UXCamSDK
{
    /// <summary>
    /// Hides this object's on-screen area in UXCam recordings while the component is enabled.
    /// </summary>
    /// <remarks>
    /// Add it to a UI element (anything with a <see cref="RectTransform"/>, for example a password
    /// <c>InputField</c>) or to a 3D/2D object with a <see cref="Renderer"/>. Unity renders into a single native
    /// surface, so the plugin computes this object's screen rectangle every frame and passes it to the
    /// native SDK, which paints over it in the recording.
    /// </remarks>
    [AddComponentMenu("UXCam/UXCam Occlude")]
    [DisallowMultipleComponent]
    public sealed class UXCamOcclude : MonoBehaviour
    {
        [Tooltip("Camera that renders this object. Leave empty to use the canvas camera for UI, or Camera.main otherwise.")]
        [SerializeField] Camera m_Camera;

        [Tooltip("Extra margin, in screen pixels, added around the rectangle so fast-moving content stays covered.")]
        [SerializeField, Min(0f)] float m_Padding = 8f;

        RectTransform m_RectTransform;
        Renderer m_Renderer;
        Canvas m_RootCanvas;

        static readonly Vector3[] s_Corners = new Vector3[8];

        /// <summary>Camera used to project this object to the screen. <c>null</c> means automatic.</summary>
        public Camera Camera
        {
            get => m_Camera;
            set => m_Camera = value;
        }

        /// <summary>Extra margin in screen pixels around the occluded rectangle.</summary>
        public float Padding
        {
            get => m_Padding;
            set => m_Padding = Mathf.Max(0f, value);
        }

        void OnEnable()
        {
            m_RectTransform = transform as RectTransform;
            m_Renderer = m_RectTransform == null ? GetComponent<Renderer>() : null;
            m_RootCanvas = null;
            if (m_RectTransform == null && m_Renderer == null)
                Debug.LogWarning($"[UXCam] UXCam Occlude on '{name}' has nothing to hide: add it to a UI element or an object with a Renderer.", this);
            UXCamRuntime.EnsureCreated();
            OcclusionRectTracker.Register(this);
        }

        void OnDisable()
        {
            OcclusionRectTracker.Unregister(this);
        }

        void OnTransformParentChanged()
        {
            m_RootCanvas = null;
        }

        internal bool TryGetScreenRect(out Rect screenRect)
        {
            screenRect = default;
            bool found = m_RectTransform != null ? TryGetUIRect(out screenRect) : TryGetRendererRect(out screenRect);
            if (!found) return false;

            screenRect.xMin -= m_Padding;
            screenRect.yMin -= m_Padding;
            screenRect.xMax += m_Padding;
            screenRect.yMax += m_Padding;
            return true;
        }

        bool TryGetUIRect(out Rect screenRect)
        {
            screenRect = default;
            if (m_RootCanvas == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                m_RootCanvas = canvas != null ? canvas.rootCanvas : null;
            }

            if (m_RootCanvas == null || !m_RootCanvas.isActiveAndEnabled) return false;

            Camera cam;
            switch (m_RootCanvas.renderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    // World corners of an overlay canvas are already screen pixels.
                    cam = null;
                    break;
                case RenderMode.ScreenSpaceCamera:
                    // Without a camera, Unity renders a Screen Space - Camera canvas as an overlay.
                    cam = m_Camera != null ? m_Camera : m_RootCanvas.worldCamera;
                    break;
                default:
                    cam = m_Camera != null ? m_Camera : (m_RootCanvas.worldCamera != null ? m_RootCanvas.worldCamera : Camera.main);
                    if (cam == null) return false;
                    break;
            }

            m_RectTransform.GetWorldCorners(s_Corners);
            return TryProject(cam, 4, m_RootCanvas.renderMode == RenderMode.WorldSpace, out screenRect);
        }

        bool TryGetRendererRect(out Rect screenRect)
        {
            screenRect = default;
            if (m_Renderer == null || !m_Renderer.enabled || !m_Renderer.isVisible) return false;

            var cam = m_Camera != null ? m_Camera : Camera.main;
            if (cam == null) return false;

            var b = m_Renderer.bounds;
            Vector3 min = b.min, max = b.max;
            s_Corners[0] = new Vector3(min.x, min.y, min.z);
            s_Corners[1] = new Vector3(min.x, min.y, max.z);
            s_Corners[2] = new Vector3(min.x, max.y, min.z);
            s_Corners[3] = new Vector3(min.x, max.y, max.z);
            s_Corners[4] = new Vector3(max.x, min.y, min.z);
            s_Corners[5] = new Vector3(max.x, min.y, max.z);
            s_Corners[6] = new Vector3(max.x, max.y, min.z);
            s_Corners[7] = new Vector3(max.x, max.y, max.z);
            return TryProject(cam, 8, true, out screenRect);
        }

        static bool TryProject(Camera cam, int cornerCount, bool cullBehindCamera, out Rect screenRect)
        {
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            int inFront = 0;
            for (int i = 0; i < cornerCount; i++)
            {
                Vector3 p = cam == null
                    ? s_Corners[i]
                    : cam.WorldToScreenPoint(s_Corners[i]);
                if (cam != null && p.z < 0f) continue;
                inFront++;
                xMin = Mathf.Min(xMin, p.x);
                yMin = Mathf.Min(yMin, p.y);
                xMax = Mathf.Max(xMax, p.x);
                yMax = Mathf.Max(yMax, p.y);
            }

            screenRect = default;
            if (inFront == 0) return false;
            if (cullBehindCamera && inFront < cornerCount)
            {
                // Partly behind the camera: the projection is unreliable, so fail safe and hide the whole screen.
                screenRect = new Rect(0f, 0f, Screen.width, Screen.height);
                return true;
            }

            screenRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            return screenRect.width > 0f && screenRect.height > 0f;
        }
    }
}
