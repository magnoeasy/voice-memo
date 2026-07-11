# OpenAI Voice Notes

## Goal

Trigger Tesla Home Music by voice, capture a note, and send that note to OpenAI for cleanup, summarization, or follow-up actions.

## Android lock-screen reality

A normal Android app cannot listen for an arbitrary always-on wake phrase while the phone is locked. The supported path is to let the system voice assistant wake the phone and hand the command to the app.

Practical trigger options:

- Google Assistant routine or shortcut launches the app with a note intent.
- A home-screen or lock-screen shortcut starts note capture after Assistant unlock/auth permits it.
- A foreground service records only after the user explicitly starts it.

The app should not try to run its own background hotword listener. That would be unreliable, battery-heavy, and blocked by Android privacy/background rules on most phones.

## Recommended flow

1. User says a configured phrase such as "Hey Google, take an OpenAI note."
2. Google Assistant opens Tesla Home Music through an Android intent or shortcut.
3. Tesla Home Music starts a note capture screen or foreground recording service.
4. The app records audio or accepts dictated text.
5. The app stores the raw note locally first.
6. The app sends the text to a small backend service.
7. The backend calls OpenAI and returns the cleaned note.
8. The app shows the cleaned note and keeps the original for audit.

## Implemented app entrypoints

Voice Memo now has a `Voice Notes` tab and accepts these Android handoffs:

- Share text to Tesla Home Music with Android `ACTION_SEND` and MIME type `text/plain`.
- Send selected text with Android `ACTION_PROCESS_TEXT`.
- Open `voxpad://voice-note`.
- Open `voxpad://voice-note?text=remember%20to%20follow%20up`.
- Open `voxpad://voice-note?note=remember%20to%20follow%20up`.

The `OpenAI notes backend` setting on the Voice Memo Voice Notes page stores the backend URL. The app posts notes there instead of calling OpenAI directly. Each saved note includes its local save date and time.

The shortest reliable Google Assistant command is usually:

```text
Hey Google, open Voice Memo
```

For hands-free use, grant microphone permission before relying on the voice command. When Voice Memo is launched normally by Assistant, it opens Voice Notes and saves the recognized text with the local date and time.

## Why use a backend

Do not put an OpenAI API key directly in the Android app. Mobile apps can be decompiled, and an embedded key should be treated as exposed.

Use a backend for:

- OpenAI API key storage.
- Rate limiting.
- User authentication.
- Request logging.
- Prompt/version control.
- Optional note sync.

## App changes

Suggested app-side components:

- `Models/VoiceNote.cs`
- `Services/IVoiceNoteStore.cs`
- `Services/VoiceNoteStore.cs`
- `Services/IOpenAiNoteClient.cs`
- `Platforms/Android/VoiceNoteIntentHandler.cs`
- A voice note page with record, review, send, and history actions.

Suggested Android manifest additions:

- microphone permission if recording inside the app.
- foreground service permission if recording continues outside the visible activity.
- intent filter or shortcut metadata for the assistant handoff.

## Backend contract

Minimal request:

```json
{
  "rawText": "remember to ask about the ChargePoint setup tomorrow",
  "createdAt": "2026-07-11T18:00:00Z",
  "source": "android-voice"
}
```

Minimal response:

```json
{
  "title": "ChargePoint setup follow-up",
  "cleanText": "Ask about the ChargePoint setup tomorrow.",
  "actions": [
    "Ask about the ChargePoint setup tomorrow."
  ]
}
```

## Open questions before implementation

- Should the note be captured as audio in the app, or dictated by Google Assistant and passed in as text?
- Should OpenAI only clean/summarize notes, or should it create action items and reminders too?
- Where should notes sync: local-only, your backend, Google Drive, OneDrive, or another store?
- Which phrase should launch the flow?
