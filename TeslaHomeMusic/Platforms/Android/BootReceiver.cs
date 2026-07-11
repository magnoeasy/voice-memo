using Android.App;
using Android.Content;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted])]
public sealed class BootReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		if (intent?.Action != Intent.ActionBootCompleted)
		{
			return;
		}

		var services = IPlatformApplication.Current?.Services;
		var settingsStore = services?.GetService<ISettingsStore>();
		var arrivalMonitor = services?.GetService<IHomeArrivalMonitor>();
		var settings = settingsStore?.Load();
		if (settings is not null)
		{
			arrivalMonitor?.Apply(settings);
		}
	}
}
