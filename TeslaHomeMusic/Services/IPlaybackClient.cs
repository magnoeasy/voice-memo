namespace TeslaHomeMusic.Services;

public interface IPlaybackClient
{
	Task PlayAsync(string musicUrl, CancellationToken cancellationToken);
}
