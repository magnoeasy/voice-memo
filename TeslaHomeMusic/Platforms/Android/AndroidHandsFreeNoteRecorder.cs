using Android.Content;
using Android.Content.PM;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic.Platforms.Android;

public sealed class AndroidHandsFreeNoteRecorder : IHandsFreeNoteRecorder
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		var context = Platform.AppContext
			?? throw new InvalidOperationException("Android context is not available.");
		if (context.CheckSelfPermission("android.permission.RECORD_AUDIO") != Permission.Granted)
		{
			throw new InvalidOperationException("Microphone permission is required for hands-free recording.");
		}

		var intent = new Intent(context, typeof(HandsFreeNoteRecordingService));

		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			context.StartForegroundService(intent);
		}
		else
		{
			context.StartService(intent);
		}

		await Task.CompletedTask;
	}
}
