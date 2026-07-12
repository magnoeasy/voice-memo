# Voice Memo

Android .NET MAUI voice-note app for hands-free capture from Google Assistant or Android share intents. It stores transcript text locally with the note date and time and can send selected notes to a backend for OpenAI processing.

## Use

The shortest Assistant command is:

```text
Hey Google, open Voice Memo
```

Voice Memo opens dictation, waits for up to 10 seconds of silence, and saves only the transcript. It does not keep the captured audio for the note workflow.

The app also accepts:

- `voxpad://voice-note`
- `voxpad://voice-note?text=remember%20this`
- Android `ACTION_SEND` and `ACTION_PROCESS_TEXT` text handoffs

## Setup

1. Install and open Voice Memo once.
2. Grant microphone and notification permissions.
3. Configure the optional transcription or notes backend endpoint.
4. For lock-screen use, configure Google Assistant to open Voice Memo and grant the required Android permissions before locking the phone.

The app does not contain an OpenAI API key. Configure the backend URL in the app; keep the OpenAI key on the server.

## Backend

The companion `VoxPadTranscriptionApi` service runs locally, accepts the app's voice-note request, calls OpenAI's Audio Transcriptions API, and returns transcript text. See `VoxPadTranscriptionApi/README.md` for setup.
