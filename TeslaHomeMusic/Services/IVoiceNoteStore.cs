using TeslaHomeMusic.Models;

namespace TeslaHomeMusic.Services;

public interface IVoiceNoteStore
{
	event EventHandler? NotesChanged;

	Task<IReadOnlyList<VoiceNote>> LoadAsync(CancellationToken cancellationToken);

	Task SaveAsync(VoiceNote note, CancellationToken cancellationToken);

	Task DeleteAllAsync(CancellationToken cancellationToken);
}
