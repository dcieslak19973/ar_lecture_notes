# XREAL One Pro / Beam Pro — Dev Environment Setup

## Overview

This project targets the **XREAL One Pro** glasses with a **Beam Pro** compute unit, building an Android AR app for college lecture note-taking. The development stack is:

- **Unity 6.4** (6000.4.4f1) with Android Build Support
- **XREAL SDK 3.1.0** (Unity XR Plugin / AR Foundation based)
- **C#** scripting (Unity's only supported language)
- **Unity Hub** for Editor and module management

---

## Prerequisites

- Windows PC
- Unity Hub installed (used for Editor install and module management)
- A Unity account (free Personal tier is sufficient)

---

## Step 1: Install Unity Hub

Unity Hub was installed via Scoop (or manually from [https://unity.com/download](https://unity.com/download)):

```powershell
scoop install unityhub
```

Or download the installer directly from Unity's website if Scoop is not available.

---

## Step 2: Install Unity 6.4 with Android Modules

There is **no separate "Unity CLI"** — the Unity Hub executable itself provides a headless CLI mode.

### List available Editor versions

```powershell
& "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless editors -r
```

### Install Unity 6.4 with Android Build Support

```powershell
& "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless install `
  --version 6000.4.4f1 `
  --module android android-sdk-ndk-tools android-open-jdk `
  --childModules
```

> **Note:** The `--` separator before `--headless` is required on Windows. Module IDs are space-separated, not comma-separated. The `--childModules` flag ensures all sub-modules (e.g., NDK, CMake) are also downloaded.

### Verify installation

```powershell
& "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless editors -i
```

Unity Editor installs to: `C:\Program Files\Unity\Hub\Editor\<version>-x86_64\`

> **Note:** On Windows x86_64, Hub creates two directories: `6000.4.4f1` (metadata only) and `6000.4.4f1-x86_64` (actual Editor + modules). The real Unity.exe is in the `-x86_64` path.

### Add modules to an existing Editor install

```powershell
& "C:\Program Files\Unity Hub\Unity Hub.exe" -- --headless install-modules `
  --version 6000.4.4f1 `
  --module android android-sdk-ndk-tools android-open-jdk `
  --childModules
```

---

## Step 3: Download XREAL SDK

Download the latest **XREAL SDK for Unity** (3.1.0 as of this writing) from:

> [https://developer.xreal.com/download](https://developer.xreal.com/download)

The SDK is a `.unitypackage` file. Import it into a Unity project via:
**Assets → Import Package → Custom Package...**

### Compatibility

| Component | Requirement |
|---|---|
| Unity | 2021.3 LTS or newer (Unity 6 supported) |
| Target platform | Android (API Level 29+) |
| Tested devices | Beam Pro, Samsung S25 |
| Architecture | ARM64 only (no 32-bit) |

---

## Step 4: Create a Unity Project

Open Unity Hub, create a new **3D (URP)** project targeting Android:

1. Open Unity Hub → **New Project**
2. Select template: **3D (URP)**
3. Set location: `D:\git\ar_lecture_notes\UnityProject`
4. In **Build Settings**: switch platform to **Android**
5. In **Player Settings**:
   - Set **Minimum API Level** to Android 10 (API 29)
   - Set **Scripting Backend** to **IL2CPP**
   - Set **Target Architecture** to **ARM64** only
   - Set **Company Name** and **Package Name** (e.g., `com.yourname.lecturnotes`)

---

## Step 5: Configure XREAL SDK

After importing the XREAL SDK `.unitypackage`:

1. Go to **Edit → Project Settings → XR Plug-in Management**
2. Under the **Android** tab, enable **XREAL**
3. Add **AR Foundation** and **XR Interaction Toolkit** packages via the Package Manager if not already present
4. Follow the [XREAL SDK docs](https://docs.xreal.com) for scene setup

---

## Running from Command Line (Batch Mode)

To build headlessly after setup:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.4f1-x86_64\Editor\Unity.exe" `
  -quit -batchmode `
  -projectPath "D:\git\ar_lecture_notes\UnityProject" `
  -buildTarget Android `
  -executeMethod BuildScript.Build
```

---

## Key Notes

- **No standalone Unity CLI exists** — use `Unity Hub.exe -- --headless` for Hub operations and `Unity.exe -batchmode` for Editor operations.
- The Hub CLI runs silently (no progress output to terminal); check logs at `%AppData%\UnityHub\logs\info-log.json`.
- You must be signed into Unity Hub with a Unity account for module installs to work.
- XREAL SDK 3.1.0 uses Unity's standard **XR Plugin / AR Foundation** APIs — not proprietary NRSDK APIs. Prefer these standard APIs for portability.
