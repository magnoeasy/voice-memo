namespace TeslaHomeMusic.Services;

public sealed class VoiceNoteRequestEventArgs(string text, bool shouldDictate, bool autoSave) : EventArgs
{
	public string Text { get; } = text;

	public bool ShouldDictate { get; } = shouldDictate;

	public bool AutoSave { get; } = autoSave;
}
