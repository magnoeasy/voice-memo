using TeslaHomeMusic.Models;

namespace TeslaHomeMusic.Services;

public interface IVoiceNoteExporter
{
	Task<string> ExportAsync(VoiceNote note, CancellationToken cancellationToken);

	Task<string> ExportAndShareAsync(VoiceNote note, CancellationToken cancellationToken);

	Task<string> ExportAllAndShareAsync(IReadOnlyList<VoiceNote> notes, CancellationToken cancellationToken);
}
