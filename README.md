# Tesla Home Music

Android .NET MAUI app that starts YouTube Music when the phone connects to a configured Tesla Bluetooth device, starts YouTube Music again when the phone connects to a configured Echo Bluetooth device, and can remind you to check ChargePoint when the phone enters a configured office radius.
It also includes a Voice Notes flow for opening the app from Android voice/share handoffs, saving dictated notes locally, and sending selected notes to an OpenAI-backed notes service. The Android app label is `Voice Memo` for clearer Google Assistant recognition.

## How It Works

- Tesla trigger: Android receives `BluetoothDevice.ActionAclConnected`, matches the configured Tesla Bluetooth name or MAC address, then launches YouTube Music.
- Home trigger: the same Bluetooth connection receiver matches the configured Echo Bluetooth name or MAC address, then launches YouTube Music.
- Office trigger: Android registers a proximity alert for the configured office latitude, longitude, and radius, then posts a local ChargePoint reminder notification.
- Playback: the app opens YouTube Music with the optional configured URL, then sends an Android media play command.
- Audio route: YouTube Music plays through Android's current audio route. For Echo playback, connect the phone to the Echo as Bluetooth audio before the home trigger runs.
- ChargePoint: tapping the office reminder opens ChargePoint when it is installed.
- Voice Notes: Android share/process-text handoffs or the `teslahomemusic://voice-note` URL open the Voice Notes tab. The app saves raw note text locally with the note date and time, then posts selected notes to the configured backend endpoint.

## Setup

1. Pair the Tesla and Echo in Android Bluetooth settings.
2. In the app, tap `Permissions`.
3. Tap `Pick Paired Tesla` and choose the Tesla.
4. Tap `Pick Paired Echo` and choose the Echo.
5. Optionally paste a YouTube Music playlist, album, or song URL.
6. Enter the office latitude, longitude, and radius.
7. Enable `office ChargePoint reminder`.
8. Tap `Save`.
9. Tap `Test ChargePoint` to verify Android notification permission and the reminder notification.
10. Enter the OpenAI notes backend endpoint, then use the `Voice Notes` tab to save or send dictated notes.

Android may require background location to be enabled from system settings before office ChargePoint reminders can run while the app is not open.
For hands-free Voice Notes, tap `Permissions` once before using the app hands-free so Android has already granted microphone access.

## Voice Notes

Configure a Google Assistant routine, shortcut, or automation to open `voxpad://voice-note`. If the automation can pass text, use `voxpad://voice-note?text=...` or share plain text to VoxPad.

The shortest reliable Assistant command is usually:

```text
Hey Google, open Voice Memo
```

When launched this way, Voice Memo opens Voice Notes, starts dictation, waits up to 10 seconds of silence, and saves the note with the local date and time.

The Android app does not store an OpenAI API key. It posts notes to your configured backend, and that backend should call OpenAI.
