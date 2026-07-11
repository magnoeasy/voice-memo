namespace TeslaHomeMusic.Models;

public sealed class OpenAiNoteRequest
{
	public string RawText { get; set; } = string.Empty;

	public DateTimeOffset CreatedAt { get; set; }

	public string Source { get; set; } = "android-voice";
}
