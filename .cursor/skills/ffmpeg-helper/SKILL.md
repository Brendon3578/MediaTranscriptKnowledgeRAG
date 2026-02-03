---
name: ffmpeg-helper
description: Manage FFmpeg setup and usage. Invoke when user asks about audio extraction, FFmpeg configuration, or media processing.
---

# FFmpeg Helper

This skill guides you through using FFmpeg in .NET applications, specifically focusing on the `FFMpegCore` wrapper and `Xabe.FFmpeg.Downloader` for binary management, based on the patterns established in `MediaTranscriptKnowledgeRAG`.

## Core Components

1. **FFMpegCore**: A .NET wrapper for FFmpeg. Used for executing commands.
2. **Xabe.FFmpeg.Downloader**: A utility to download the latest FFmpeg binaries. Used for bootstrapping.

## 1. Bootstrapping FFmpeg

Ensure FFmpeg binaries are available before execution. Use a `Bootstrapper` or `Startup` service.

### Pattern: Check and Download

```csharp
using Xabe.FFmpeg.Downloader;
using FFMpegCore;

public async Task InitializeAsync(CancellationToken cancellationToken)
{
    // 1. Determine target directory
    var ffmpegDir = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
    if (!Directory.Exists(ffmpegDir))
    {
        Directory.CreateDirectory(ffmpegDir);
    }

    // 2. Check if executable exists
    var ffmpegExe = Path.Combine(ffmpegDir, "ffmpeg.exe");
    if (!File.Exists(ffmpegExe))
    {
        // 3. Download if missing
        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegDir);
    }

    // 4. Configure FFMpegCore to use this directory
    GlobalFFOptions.Configure(new FFOptions { BinaryFolder = ffmpegDir });
}
```

## 2. Audio Extraction (for Whisper)

Whisper typically requires **16kHz, Mono, WAV** audio.

### Pattern: Extract Audio

```csharp
using FFMpegCore;

public async Task<string> ExtractAudioAsync(string videoPath, string outputPath)
{
    // Ensure output directory exists
    // ...

    await FFMpegArguments
        .FromFileInput(videoPath)
        .OutputToFile(outputPath, true, options => options
            .WithAudioSamplingRate(16000)
            .WithAudioCodec("pcm_s16le")
            .ForceFormat("wav")
            .WithCustomArgument("-ac 1")) // Mono
        .ProcessAsynchronously();

    return outputPath;
}
```

## 3. Configuration

Avoid hardcoding paths. Use `appsettings.json` or environment variables.

**appsettings.json**:

```json
{
  "FFmpeg": {
    "ExecutablePath": "ffmpeg" // Relative folder name or absolute path
  }
}
```

**Loading Configuration**:

```csharp
var executablePath = configuration["FFmpeg:ExecutablePath"] ?? "ffmpeg";
// Logic to resolve full path...
```

## 4. Dependencies

Add these packages to your project:

```xml
<PackageReference Include="FFMpegCore" Version="5.4.0" />
<PackageReference Include="Xabe.FFmpeg.Downloader" Version="6.0.2" />
```

## Common Issues

* **Path Resolution**: Ensure `GlobalFFOptions.Configure` is called *before* any `FFMpegArguments` usage.
* **Permissions**: Ensure the process has write permissions to the `ffmpeg` directory for downloading.
* **Platform**: `Xabe.FFmpeg.Downloader` detects the OS and downloads the appropriate binaries (Windows/Linux/MacOS).
