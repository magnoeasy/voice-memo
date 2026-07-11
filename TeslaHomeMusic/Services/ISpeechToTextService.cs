namespace TeslaHomeMusic.Services;

public interface ISpeechToTextService
{
	Task<string> ListenAsync(CancellationToken cancellationToken);
}
