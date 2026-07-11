namespace TeslaHomeMusic.Models;

public sealed class OpenAiNoteResponse
{
	public string Title { get; set; } = string.Empty;

	public string CleanText { get; set; } = string.Empty;

	public IReadOnlyList<string> Actions { get; set; } = [];
}
