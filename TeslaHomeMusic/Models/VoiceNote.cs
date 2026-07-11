namespace TeslaHomeMusic.Models;

public sealed class VoiceNote
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

	public string RawText { get; set; } = string.Empty;

	public string Title { get; set; } = string.Empty;

	public string CleanText { get; set; } = string.Empty;

	public string AudioPath { get; set; } = string.Empty;

	public IReadOnlyList<string> Actions { get; set; } = [];

	public bool IsShared { get; set; }
}
