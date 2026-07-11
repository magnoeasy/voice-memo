using Android.App;
using Android.Bluetooth;
using Android.Content;
using Java.Lang;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([BluetoothDevice.ActionAclConnected])]
public sealed class BluetoothConnectionReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		if (intent?.Action != BluetoothDevice.ActionAclConnected)
		{
			return;
		}

#pragma warning disable CA1422
		var device = intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) as BluetoothDevice;
#pragma warning restore CA1422
		if (device is null)
		{
			return;
		}

		var pendingResult = GoAsync();
		if (pendingResult is null)
		{
			return;
		}

		_ = HandleAsync(device, pendingResult);
	}

	private static async Task HandleAsync(BluetoothDevice device, PendingResult pendingResult)
	{
		try
		{
			var services = IPlatformApplication.Current?.Services;
			var coordinator = services?.GetService<IAutomationCoordinator>();
			if (coordinator is not null)
			{
				await coordinator.BluetoothConnectedAsync(ReadName(device), device.Address ?? string.Empty, CancellationToken.None).ConfigureAwait(false);
			}
		}
		finally
		{
			pendingResult.Finish();
		}
	}

	private static string ReadName(BluetoothDevice device)
	{
		try
		{
			return device.Name ?? "Unknown";
		}
		catch (SecurityException)
		{
			return "Unknown";
		}
	}
}
