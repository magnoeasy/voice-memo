namespace TeslaHomeMusic.Services;

public interface IVoiceNoteInbox
{
	event EventHandler<VoiceNoteRequestEventArgs>? VoiceRequested;

	void Put(string text);

	string Take();

	void RequestDictation(bool autoSave);

	bool TakeDictationRequest(out bool autoSave);
}
