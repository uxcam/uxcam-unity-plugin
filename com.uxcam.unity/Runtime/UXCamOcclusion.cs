using System.Collections.Generic;
using UnityEngine;

namespace UXCamSDK
{
    /// <summary>
    /// A whole-screen occlusion rule: blur or overlay entire screens, or hide native text fields.
    /// Pass it in <see cref="UXCamConfiguration.Occlusions"/> or to <see cref="UXCam.ApplyOcclusion"/>.
    /// To hide part of the screen, use the <see cref="UXCamOcclude"/> component.
    /// </summary>
    public abstract class UXCamOcclusion
    {
        // Values match UXOcclusionType on iOS and the type ids used by the Android bridge.
        internal const int TypeOccludeAllTextFields = 1;
        internal const int TypeOverlay = 2;
        internal const int TypeBlur = 3;

        internal abstract int Type { get; }

        /// <summary>Screen names this rule applies to. Empty means every screen.</summary>
        public List<string> Screens { get; } = new List<string>();

        /// <summary>When <c>true</c>, the rule applies to every screen except <see cref="Screens"/>.</summary>
        public bool ExcludeMentionedScreens { get; set; }

        internal virtual Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["type"] = Type,
                ["screens"] = new List<string>(Screens),
                ["excludeMentionedScreens"] = ExcludeMentionedScreens,
            };
        }
    }

    /// <summary>Blur algorithm. iOS only; Android always uses its default blur.</summary>
    public enum UXCamBlurType
    {
        Gaussian = 0,
        Box = 1,
        Bokeh = 2,
    }

    /// <summary>Blurs the screen in the recording.</summary>
    public sealed class UXCamBlur : UXCamOcclusion
    {
        internal override int Type => TypeBlur;

        /// <summary>Blur radius. Default 10.</summary>
        public int BlurRadius { get; set; } = 10;

        /// <summary>iOS only. Default <see cref="UXCamBlurType.Gaussian"/>.</summary>
        public UXCamBlurType BlurType { get; set; } = UXCamBlurType.Gaussian;

        /// <summary>Also stop recording touches while the blur is applied. Default <c>true</c>.</summary>
        public bool HideGestures { get; set; } = true;

        internal override Dictionary<string, object> ToDictionary()
        {
            var json = base.ToDictionary();
            json["blurRadius"] = BlurRadius;
            json["blurType"] = (int)BlurType;
            json["hideGestures"] = HideGestures;
            return json;
        }
    }

    /// <summary>Covers the screen with a solid colour in the recording.</summary>
    public sealed class UXCamOverlay : UXCamOcclusion
    {
        internal override int Type => TypeOverlay;

        /// <summary>iOS only; Android always uses its default colour. Default black.</summary>
        public Color Color { get; set; } = Color.black;

        /// <summary>Also stop recording touches while the overlay is applied. Default <c>true</c>.</summary>
        public bool HideGestures { get; set; } = true;

        internal override Dictionary<string, object> ToDictionary()
        {
            var json = base.ToDictionary();
            Color32 c = Color;
            json["color"] = new List<object> { (int)c.r, (int)c.g, (int)c.b, (int)c.a };
            json["hideGestures"] = HideGestures;
            return json;
        }
    }

    /// <summary>
    /// Hides native text fields, such as the input box shown over the keyboard for <see cref="TouchScreenKeyboard"/>.
    /// </summary>
    public sealed class UXCamOccludeAllTextFields : UXCamOcclusion
    {
        internal override int Type => TypeOccludeAllTextFields;
    }
}
