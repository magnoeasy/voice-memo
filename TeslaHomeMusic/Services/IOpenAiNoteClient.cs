using TeslaHomeMusic.Models;

namespace TeslaHomeMusic.Services;

public interface IOpenAiNoteClient
{
	Task<OpenAiNoteResponse> ShareAsync(VoiceNote note, CancellationToken cancellationToken);

	Task<OpenAiNoteResponse> TranscribeAsync(VoiceNote note, CancellationToken cancellationToken);
}
