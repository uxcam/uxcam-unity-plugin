using System.Collections.Generic;
using UnityEngine;

namespace UXCamSDK.Internal
{
    internal static class OcclusionRectTracker
    {
        // Rects are compared at this precision so that sub-pixel jitter doesn't cause a native call every frame.
        const float Quantum = 1f / 4096f;

        static readonly List<UXCamOcclude> s_Components = new List<UXCamOcclude>();
        static readonly Dictionary<string, Rect[]> s_ManualRects = new Dictionary<string, Rect[]>();
        static readonly List<float> s_Current = new List<float>(64);
        static float[] s_LastSent = new float[0];

        internal static void Register(UXCamOcclude component)
        {
            if (!s_Components.Contains(component)) s_Components.Add(component);
        }

        internal static void Unregister(UXCamOcclude component)
        {
            s_Components.Remove(component);
        }

        internal static void SetManualRects(string id, IEnumerable<Rect> rects)
        {
            if (rects == null)
                s_ManualRects.Remove(id);
            else
                s_ManualRects[id] = new List<Rect>(rects).ToArray();
        }

        internal static void Flush(IUXCamPlatform platform, float screenWidth, float screenHeight)
        {
            if (screenWidth <= 0f || screenHeight <= 0f) return;

            s_Current.Clear();
            for (int i = s_Components.Count - 1; i >= 0; i--)
            {
                var component = s_Components[i];
                if (component == null)
                {
                    s_Components.RemoveAt(i);
                    continue;
                }

                if (component.TryGetScreenRect(out var rect))
                    AddNormalized(rect, screenWidth, screenHeight);
            }

            foreach (var rects in s_ManualRects.Values)
            {
                foreach (var rect in rects)
                    AddNormalized(rect, screenWidth, screenHeight);
            }

            if (SameAsLastSent()) return;

            s_LastSent = s_Current.ToArray();
            platform.SetOcclusionRects(s_LastSent);
        }

        internal static bool TryNormalize(Rect rect, float screenWidth, float screenHeight, out Rect normalized)
        {
            float xMin = Mathf.Clamp01(rect.xMin / screenWidth);
            float xMax = Mathf.Clamp01(rect.xMax / screenWidth);
            float yTop = Mathf.Clamp01(1f - rect.yMax / screenHeight);
            float yBottom = Mathf.Clamp01(1f - rect.yMin / screenHeight);

            xMin = Quantize(xMin);
            yTop = Quantize(yTop);
            xMax = Quantize(xMax);
            yBottom = Quantize(yBottom);

            normalized = Rect.MinMaxRect(xMin, yTop, xMax, yBottom);
            return normalized.width > 0f && normalized.height > 0f;
        }

        static void AddNormalized(Rect rect, float screenWidth, float screenHeight)
        {
            if (!TryNormalize(rect, screenWidth, screenHeight, out var n)) return;
            s_Current.Add(n.x);
            s_Current.Add(n.y);
            s_Current.Add(n.width);
            s_Current.Add(n.height);
        }

        static float Quantize(float value) => Mathf.Round(value / Quantum) * Quantum;

        static bool SameAsLastSent()
        {
            if (s_Current.Count != s_LastSent.Length) return false;
            for (int i = 0; i < s_LastSent.Length; i++)
            {
                if (!Mathf.Approximately(s_Current[i], s_LastSent[i])) return false;
            }
            return true;
        }

        internal static void Reset()
        {
            s_Components.Clear();
            s_ManualRects.Clear();
            s_Current.Clear();
            s_LastSent = new float[0];
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();
    }
}
