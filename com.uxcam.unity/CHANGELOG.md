# Changelog

All notable changes to this package are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this package follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

First release of the UPM-based UXCam Unity plugin, replacing the earlier copy-in plugin (`uxcam/uxcam-unity`).

### Added
- `UXCam` static API for iOS and Android: sessions, opt-in/out, screen tagging, user and session properties,
  events, bug and exception events, and occlusion. Calls are no-ops in the Editor and on other platforms.
- Project Settings > UXCam with automatic start, per-platform app keys and native SDK options.
- Automatic screen tagging from the active Unity scene.
- `UXCam Occlude` component and `UXCam.SetOcclusionRects` to hide parts of the Unity-rendered screen, with a
  Game view preview in Play Mode.
- Optional reporting of unhandled C# exceptions as UXCam exception events.
- `OptIntoVideoRecording` configuration and Project Settings option, applied natively right before start. iOS
  records no screen video without an opt-in.
- Native dependencies resolved by Unity's External Dependency Manager: iOS `uxcam-ios` 3.11.1 (Swift Package
  Manager) and Android `com.uxcam:uxcam:3.11.2`.
