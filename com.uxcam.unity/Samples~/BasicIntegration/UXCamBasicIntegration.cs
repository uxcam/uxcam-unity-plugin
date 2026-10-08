using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UXCamSDK;

namespace UXCamSDK.Samples
{
    /// <summary>
    /// Add to any GameObject in a scene. Builds a small UI at runtime, starts UXCam from code (unless it already
    /// started from Project Settings &gt; UXCam), and hides the password field in recordings.
    /// </summary>
    public sealed class UXCamBasicIntegration : MonoBehaviour
    {
        [Tooltip("Your UXCam app key. Leave empty to use the key from Project Settings > UXCam.")]
        [SerializeField] string m_AppKey = string.Empty;

        void Start()
        {
            BuildUI();

            if (UXCam.Configuration != null)
            {
                Debug.Log("[UXCam sample] Already started from UXCamSettings; skipping the code start.");
                return;
            }

            var settings = UXCamSettings.Load();
            UXCamConfiguration configuration;
            if (!string.IsNullOrWhiteSpace(m_AppKey))
                configuration = new UXCamConfiguration(m_AppKey);
            else if (settings != null)
                configuration = settings.ToConfiguration(Application.platform);
            else
            {
                Debug.LogWarning("[UXCam sample] Set an app key on this component or in Project Settings > UXCam.");
                return;
            }

            // Blur every screen named "Payment" in addition to per-element occlusion.
            var paymentBlur = new UXCamBlur { BlurRadius = 15 };
            paymentBlur.Screens.Add("Payment");
            configuration.Occlusions.Add(paymentBlur);

            UXCam.StartWithConfiguration(configuration, verified =>
                Debug.Log(verified
                    ? "[UXCam sample] Recording. Session: " + UXCam.UrlForCurrentSession()
                    : "[UXCam sample] Not recording (Editor, or the app key failed verification)."));

            UXCam.SetUserIdentity("sample-user-42");
            UXCam.SetUserProperty("plan", "premium");
            UXCam.SetUserProperty("level", 7);
        }

        void BuildUI()
        {
            EnsureEventSystem();

            var canvasObject = new GameObject("UXCam Sample Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var password = CreateInputField(canvas.transform, font, "Password", new Vector2(0f, 120f));
            password.contentType = InputField.ContentType.Password;
            // Hides the field's on-screen rectangle in every recorded frame while the component is enabled.
            password.gameObject.AddComponent<UXCamOcclude>();

            CreateButton(canvas.transform, font, "Buy (logs an event)", new Vector2(0f, 0f), () =>
                UXCam.LogEvent("Purchase", new Dictionary<string, object> { ["item"] = "gems", ["amount"] = 500, ["price"] = 4.99 }));

            CreateButton(canvas.transform, font, "Open \"Payment\" screen", new Vector2(0f, -120f), () =>
                UXCam.TagScreenName("Payment"));
        }

        // Unity 6 projects default to the Input System package, where StandaloneInputModule throws; pick whichever
        // module the project supports without referencing the Input System assembly.
        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule != null)
            {
                eventSystem.AddComponent(inputSystemModule);
                return;
            }
#endif
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        static InputField CreateInputField(Transform parent, Font font, string placeholder, Vector2 position)
        {
            var root = CreateRect("InputField", parent, position, new Vector2(600f, 90f));
            root.gameObject.AddComponent<Image>().color = Color.white;
            var input = root.gameObject.AddComponent<InputField>();

            var text = CreateText(root, font, string.Empty, Color.black);
            var hint = CreateText(root, font, placeholder, new Color(0f, 0f, 0f, 0.4f));
            input.textComponent = text;
            input.placeholder = hint;
            return input;
        }

        static void CreateButton(Transform parent, Font font, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            var root = CreateRect("Button", parent, position, new Vector2(600f, 90f));
            root.gameObject.AddComponent<Image>().color = new Color(0.36f, 0.27f, 0.93f);
            root.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
            CreateText(root, font, label, Color.white).alignment = TextAnchor.MiddleCenter;
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static Text CreateText(RectTransform parent, Font font, string value, Color color)
        {
            var rect = CreateRect("Text", parent, Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(20f, 0f);
            rect.offsetMax = new Vector2(-20f, 0f);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 36;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.text = value;
            return text;
        }
    }
}
