namespace TeslaHomeMusic.Services;

public sealed class AutomationCoordinator(
	ISettingsStore settingsStore,
	IPlaybackClient playback,
	IChargePointNotifier chargePoint) : IAutomationCoordinator
{
	public async Task BluetoothConnectedAsync(string deviceName, string bluetoothAddress, CancellationToken cancellationToken)
	{
		var settings = settingsStore.Load();
		if (IsDevice(settings.TeslaDeviceName, settings.TeslaBluetoothAddress, deviceName, bluetoothAddress))
		{
			await playback.PlayAsync(settings.MusicUrl, cancellationToken).ConfigureAwait(false);
			settingsStore.SetStatus($"Started YouTube Music for Tesla connection {deviceName}.");
			return;
		}

		if (settings.IsHomeEnabled && IsDevice(settings.EchoDeviceName, settings.EchoBluetoothAddress, deviceName, bluetoothAddress))
		{
			await playback.PlayAsync(settings.MusicUrl, cancellationToken).ConfigureAwait(false);
			settingsStore.SetStatus($"Started YouTube Music for Echo connection {deviceName}.");
			return;
		}

		settingsStore.SetStatus($"Ignored Bluetooth device {deviceName}.");
	}

	public Task OfficeEnteredAsync(CancellationToken cancellationToken)
	{
		var settings = settingsStore.Load();
		if (!settings.IsChargePointReminderEnabled)
		{
			settingsStore.SetStatus("Ignored office arrival because ChargePoint reminder is disabled.");
			return Task.CompletedTask;
		}

		chargePoint.NotifyAvailableCheck();
		settingsStore.SetStatus("Requested ChargePoint and posted an office-arrival reminder.");
		return Task.CompletedTask;
	}

	private static bool IsDevice(string expectedName, string expectedAddress, string deviceName, string bluetoothAddress)
	{
		if (!string.IsNullOrWhiteSpace(expectedAddress))
		{
			return string.Equals(expectedAddress.Trim(), bluetoothAddress, StringComparison.OrdinalIgnoreCase);
		}

		return deviceName.Contains(expectedName, StringComparison.OrdinalIgnoreCase);
	}
}
