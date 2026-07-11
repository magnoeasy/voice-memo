namespace TeslaHomeMusic.Services;

public sealed class VoiceNoteInbox : IVoiceNoteInbox
{
	private string pendingText = string.Empty;
	private bool shouldDictate;
	private bool shouldAutoSave;

	public event EventHandler<VoiceNoteRequestEventArgs>? VoiceRequested;

	public void Put(string text)
	{
		pendingText = text.Trim();
		VoiceRequested?.Invoke(this, new VoiceNoteRequestEventArgs(pendingText, shouldDictate: false, autoSave: false));
	}

	public string Take()
	{
		var text = pendingText;
		pendingText = string.Empty;
		return text;
	}

	public void RequestDictation(bool autoSave)
	{
		shouldDictate = true;
		shouldAutoSave = autoSave;
		VoiceRequested?.Invoke(this, new VoiceNoteRequestEventArgs(string.Empty, shouldDictate: true, autoSave));
	}

	public bool TakeDictationRequest(out bool autoSave)
	{
		var value = shouldDictate;
		autoSave = shouldAutoSave;
		shouldDictate = false;
		shouldAutoSave = false;
		return value;
	}
}
