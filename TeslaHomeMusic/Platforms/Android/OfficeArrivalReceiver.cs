using Android.App;
using Android.Content;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
[IntentFilter(["com.fernandoalves.teslahomemusic.OFFICE_ENTERED"])]
public sealed class OfficeArrivalReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		var pendingResult = GoAsync();
		if (pendingResult is null)
		{
			return;
		}

		_ = HandleAsync(pendingResult);
	}

	private static async Task HandleAsync(PendingResult pendingResult)
	{
		try
		{
			var services = IPlatformApplication.Current?.Services;
			var coordinator = services?.GetService<IAutomationCoordinator>();
			if (coordinator is not null)
			{
				await coordinator.OfficeEnteredAsync(CancellationToken.None).ConfigureAwait(false);
			}
		}
		finally
		{
			pendingResult.Finish();
		}
	}
}
