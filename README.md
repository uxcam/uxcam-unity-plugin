# UXCam for Unity

Session replay, heatmaps and product analytics for Unity games and apps on **iOS** and **Android**. This package
wraps the native UXCam iOS and Android SDKs and adds what a Unity app needs on top: scene-based screen names,
hiding sensitive Unity UI in recordings, and Project Settings integration.

## Requirements

| | Minimum |
|---|---|
| Unity | 2022.3 LTS or later |
| iOS | 12.0 (or Unity's own minimum for your editor version, whichever is higher) |
| Android | API 21 (or Unity's own minimum for your editor version, whichever is higher) |
| Dependencies | Unity's External Dependency Manager (`com.unity.external-dependency-manager` 2.1+), installed automatically |

In the Editor and on other platforms every call is a safe no-op, so you don't need `#if` around UXCam code.

## Install

**Package Manager, from git URL** (*Window > Package Manager > + > Add package from git URL*):

```
https://github.com/uxcam/uxcam-unity-plugin.git?path=com.uxcam.unity#v1.0.0
```

Or add it to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.uxcam.unity": "https://github.com/uxcam/uxcam-unity-plugin.git?path=com.uxcam.unity#v1.0.0"
  }
}
```

The External Dependency Manager (EDM) is installed with the package and adds the native SDKs to your builds:

- **iOS:** the [`uxcam-ios`](https://github.com/uxcam/uxcam-ios) Swift package. Keep **Swift Package Manager
  Integration** on in *Project Settings > External Dependency Manager > iOS Resolver*. The plugin doesn't use
  CocoaPods.
- **Android:** `com.uxcam:uxcam` from Maven Central. EDM writes it into your Gradle templates;
  if it asks, enable **Custom Main Gradle Template** and **Custom Gradle Settings Template** in
  *Player Settings > Android > Publishing Settings*.

> **Already using Google's EDM4U** (`com.google.external-dependency-manager`)? Unity's
> EDM replaces it and will offer to switch when both are present. See Unity's
> [EDM migration guide](https://docs.unity3d.com/Packages/com.unity.external-dependency-manager@2.1/manual/get-started-with-edm.html).

## Quick start

1. Open **Edit > Project Settings > UXCam** and click **Create UXCam Settings**.
2. Paste your app key from the UXCam dashboard. If iOS and Android are separate UXCam apps, set the per-platform
   overrides.
3. Decide on **Opt Into Video Recording**. iOS records no screen video until the device is opted in; Android records
   video by default. Turn it on if you have the user's consent, or call `UXCam.OptIntoVideoRecording()` once they
   give it.
4. Build to a device. UXCam starts after the first scene loads, and each active scene is recorded as a screen.

To start from code instead, for example after the user gives consent, turn off **Start Automatically** and call:

```csharp
using UXCamSDK;

var configuration = new UXCamConfiguration("YOUR_APP_KEY")
{
    OptIntoVideoRecording = userConsentedToRecording, // iOS records no video without it
    EnableIntegrationLogging = Debug.isDebugBuild,
};
UXCam.StartWithConfiguration(configuration, verified => Debug.Log($"UXCam recording: {verified}"));

// Or reuse the Project Settings values:
UXCam.StartWithConfiguration(UXCamSettings.Load().ToConfiguration(Application.platform));
```

The callback runs on the Unity main thread, so it can use Unity APIs directly.

To remove the automatic start code path entirely, add the scripting define `UXCAM_DISABLE_AUTO_INIT`.

For a working example, import *Package Manager > UXCam > Samples > Basic Integration*.

## Screens

Unity renders the whole game into one native screen, so native automatic screen tagging is **off** by default and
the plugin tags the **active scene's name** whenever it changes (`TagScenesAutomatically`). Tag finer-grained
screens, such as menus and dialogs within a scene, yourself:

```csharp
UXCam.TagScreenName("Shop");
UXCam.AddScreenNameToIgnore("Cutscene");   // no video while this screen is visible
```

## Events and properties

```csharp
UXCam.SetUserIdentity(player.Id);
UXCam.SetUserProperty("plan", "premium");
UXCam.SetSessionProperty("level", 12);

UXCam.LogEvent("Purchase", new Dictionary<string, object>
{
    ["item"] = "gems",
    ["amount"] = 500,
    ["price"] = 4.99,
});
```

Property values can be strings, numbers, booleans, or nested dictionaries and lists of those.

## Hiding sensitive content

Native SDKs can hide native views, but everything Unity draws is one surface. Use one of these instead.

**Hide part of the screen: the `UXCam Occlude` component.** Add *UXCam > UXCam Occlude* to any UI element
(for example a password `InputField` or a `TMP_InputField`) or to a 3D/2D object with a `Renderer`. While the
component is enabled, the plugin computes the object's screen rectangle at the end of every frame and the native SDK
paints over it in the recording. `Padding` adds a margin for fast-moving content. If a 3D object is partly behind
the camera, the whole screen is hidden for that frame, to fail safe. In Play Mode, hidden areas are outlined in red
in the Game view.

```csharp
passwordField.gameObject.AddComponent<UXCamOcclude>();
```

For content without a GameObject, such as an `OnGUI` panel, pass rectangles in screen pixels (origin bottom-left):

```csharp
UXCam.SetOcclusionRects("debug-panel", new[] { new Rect(0, 0, 400, 300) });
UXCam.ClearOcclusionRects("debug-panel");
```

**Hide whole screens.** Hide everything while sensitive data is visible:

```csharp
UXCam.OccludeSensitiveScreen(true);
// ...
UXCam.OccludeSensitiveScreen(false);
```

Or blur/overlay named screens from the first frame of every session:

```csharp
var blur = new UXCamBlur { BlurRadius = 15 };
blur.Screens.Add("Payment");
configuration.Occlusions.Add(blur);
// At runtime: UXCam.ApplyOcclusion(blur) / UXCam.RemoveOcclusion(blur)
```

**Native text fields.** On iOS and Android, `TouchScreenKeyboard` shows a native input box above the keyboard.
Hide it with `UXCam.OccludeAllTextFields(true)` or a `UXCamOccludeAllTextFields` occlusion.

## Consent

```csharp
UXCam.OptIntoVideoRecording();     // required for video on iOS; restarts a running session
UXCam.OptOutOfVideoRecording();    // analytics only, no screen video
UXCam.OptOutOverall();             // no data at all
UXCam.OptInVideoRecordingStatus();
```

All of these persist on the device until the app is reinstalled. Video opt-in defaults differ: **off on iOS**, on
for Android. To opt in at launch without restarting the first session, set `OptIntoVideoRecording` on the
configuration (or in Project Settings) instead of calling the method.

## Errors

`UXCam.ReportExceptionEvent(exception)` attaches a handled C# exception and its stack trace to the session.
Turn on **Report Unhandled C# Exceptions** (or `CaptureUnhandledExceptions`) to report the exceptions Unity logs
as unhandled; identical exceptions are reported at most once every 5 seconds. Native crashes are captured by the
native SDKs (`EnableCrashHandling`).

## Platform differences

| Feature | iOS | Android |
|---|---|---|
| Screen video before any opt-in call | Off: call `OptIntoVideoRecording` | On |
| `EnableAdvancedGestureRecognition`, `EnableNetworkLogging`, `EnableImprovedScreenCapture` | Yes | Ignored |
| `UXCamBlur.BlurType` | Yes | Default blur only |
| `UXCamOverlay.Color` | Yes | Fixed colour |
| `ExcludeMentionedScreens` on `ApplyOcclusion` at runtime | Configuration only | Yes |

## Troubleshooting

- **Nothing in the dashboard from the Editor.** Expected: UXCam records on iOS and Android devices only. Turn on
  *Log Calls in the Editor* to see the calls in the Console.
- **Android build: `package com.uxcam does not exist`.** EDM hasn't added the dependency to Gradle. Run the
  Android Resolver from *Assets > External Dependency Manager* and check the Gradle templates (see Install).
- **iOS build: `'UXCam/UXCam.h' file not found` or undefined `UXCam` symbols.** The Swift package wasn't added
  to the Xcode project. Turn on **Swift Package Manager Integration** in *Project Settings > External Dependency
  Manager > iOS Resolver*, then build the Xcode project again.
- **Integration logs.** Turn on *Integration Logging* and watch Xcode or `adb logcat -s UXCam UXCamUnity`.

## License

BSD 3-Clause. See [LICENSE](LICENSE).
