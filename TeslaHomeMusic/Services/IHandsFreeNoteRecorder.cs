namespace TeslaHomeMusic.Services;

public interface IHandsFreeNoteRecorder
{
	Task StartAsync(CancellationToken cancellationToken);
}
