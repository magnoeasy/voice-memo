# VoxPad transcription service

This local service keeps the OpenAI API key off the Android device.

In PowerShell, set the key for the current session and start the service:

```powershell
$env:OPENAI_API_KEY = "your-api-key"
dotnet run --project .\VoxPadTranscriptionApi\VoxPadTranscriptionApi.csproj
```

Find the computer's Wi-Fi IPv4 address with `ipconfig`, then enter this URL in VoxPad:

```text
http://YOUR-PC-IP:5080/voice-notes
```

The phone and computer must be on the same network. The service uploads audio to OpenAI's Audio Transcriptions API using `gpt-4o-transcribe` and returns the text in VoxPad's response format.
