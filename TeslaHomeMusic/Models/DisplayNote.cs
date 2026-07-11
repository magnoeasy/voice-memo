namespace TeslaHomeMusic.Models;

public sealed class DisplayNote
{
	public required VoiceNote Note { get; init; }

	public required DateTimeOffset CreatedAt { get; init; }

	public required string Title { get; init; }

	public required string RawText { get; init; }

	public static DisplayNote From(VoiceNote note)
	{
		var title = !string.IsNullOrWhiteSpace(note.Title)
			? note.Title
			: "Untitled note";

		return new DisplayNote
		{
			Note = note,
			CreatedAt = note.CreatedAt,
			Title = note.IsShared ? $"{title} (shared)" : title,
			RawText = ReadDisplayText(note)
		};
	}

	private static string ReadDisplayText(VoiceNote note)
	{
		if (!string.IsNullOrWhiteSpace(note.CleanText))
		{
			return note.CleanText;
		}

		if (!string.IsNullOrWhiteSpace(note.RawText))
		{
			return note.RawText;
		}

		return string.IsNullOrWhiteSpace(note.AudioPath) ? string.Empty : "Audio note saved.";
	}
}
