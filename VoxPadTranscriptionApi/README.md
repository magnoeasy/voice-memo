# VoxPad Transcription API

Local ASP.NET Core service for the Voice Memo Android app. It keeps the OpenAI API key off the phone and returns transcript text to the app.

## Requirements

- .NET 10 SDK
- An OpenAI API key
- Phone and computer on the same Wi-Fi network

## Run

In PowerShell, set the key for the current session and start the service:

```powershell
$env:OPENAI_API_KEY = "your-api-key"
dotnet run --project .\VoxPadTranscriptionApi\VoxPadTranscriptionApi.csproj
```

The API listens on port `5080`. Find the computer's Wi-Fi IPv4 address with `ipconfig`, then enter this URL in Voice Memo:

```text
http://YOUR-PC-IP:5080/voice-notes
```

The phone and computer must be on the same network. The service uploads audio to OpenAI's Audio Transcriptions API using `gpt-4o-transcribe` and returns the transcript in the app's response format.

Never put `OPENAI_API_KEY` in the Android application or commit it to source control.
