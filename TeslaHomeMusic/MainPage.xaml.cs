using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using TeslaHomeMusic.Models;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic;

public partial class MainPage : ContentPage
{
	private readonly ISettingsStore settingsStore;
	private readonly IHomeArrivalMonitor homeArrivalMonitor;
	private readonly IPermissionService permissionService;
	private readonly IAutomationCoordinator automationCoordinator;
	private readonly IBluetoothDeviceStore bluetoothDeviceStore;

	public MainPage()
	{
		InitializeComponent();

		var services = IPlatformApplication.Current?.Services
			?? throw new InvalidOperationException("Application services are not available.");

		settingsStore = services.GetRequiredService<ISettingsStore>();
		homeArrivalMonitor = services.GetRequiredService<IHomeArrivalMonitor>();
		permissionService = services.GetRequiredService<IPermissionService>();
		automationCoordinator = services.GetRequiredService<IAutomationCoordinator>();
		bluetoothDeviceStore = services.GetRequiredService<IBluetoothDeviceStore>();

		LoadSettings();
	}

	private void LoadSettings()
	{
		var settings = settingsStore.Load();
		TeslaDeviceNameEntry.Text = settings.TeslaDeviceName;
		TeslaBluetoothAddressEntry.Text = settings.TeslaBluetoothAddress;
		EchoDeviceNameEntry.Text = settings.EchoDeviceName;
		EchoBluetoothAddressEntry.Text = settings.EchoBluetoothAddress;
		HomeEnabledSwitch.IsToggled = settings.IsHomeEnabled;
		MusicUrlEntry.Text = settings.MusicUrl;
		OfficeLatitudeEntry.Text = FormatCoordinate(settings.OfficeLatitude);
		OfficeLongitudeEntry.Text = FormatCoordinate(settings.OfficeLongitude);
		OfficeRadiusEntry.Text = settings.OfficeRadiusMeters.ToString(CultureInfo.InvariantCulture);
		ChargePointReminderEnabledSwitch.IsToggled = settings.IsChargePointReminderEnabled;
		ChargePointStationNameEntry.Text = settings.ChargePointStationName;
		ChargePointAlertAutomationEnabledSwitch.IsToggled = settings.IsChargePointAlertAutomationEnabled;
		OpenAiNotesEndpointEntry.Text = settings.OpenAiNotesEndpoint;
		RefreshStatus();
	}

	private async void OnPermissionsClicked(object? sender, EventArgs e)
	{
		var granted = await permissionService.RequestAutomationPermissionsAsync();
		settingsStore.SetStatus(granted ? "Automation permissions granted." : "Automation permissions are incomplete.");
		RefreshStatus();
	}

	private void OnSaveClicked(object? sender, EventArgs e)
	{
		var settings = ReadSettings();
		settingsStore.Save(settings);
		homeArrivalMonitor.Apply(settings);
		settingsStore.SetStatus("Settings saved and home arrival monitor updated.");
		RefreshStatus();
	}

	private async void OnPickTeslaClicked(object? sender, EventArgs e)
	{
		var device = await PickPairedDeviceAsync("Choose Tesla");
		if (device is null)
		{
			return;
		}

		TeslaDeviceNameEntry.Text = device.Name;
		TeslaBluetoothAddressEntry.Text = device.Address;
		SaveWithoutRegisteringHome("Tesla Bluetooth device selected.");
	}

	private async void OnPickEchoClicked(object? sender, EventArgs e)
	{
		var device = await PickPairedDeviceAsync("Choose Echo");
		if (device is null)
		{
			return;
		}

		EchoDeviceNameEntry.Text = device.Name;
		EchoBluetoothAddressEntry.Text = device.Address;
		SaveWithoutRegisteringHome("Echo Bluetooth device selected.");
	}

	private async void OnTestTeslaClicked(object? sender, EventArgs e)
	{
		try
		{
			var settings = ReadSettings();
			settingsStore.Save(settings);
			await automationCoordinator.BluetoothConnectedAsync(settings.TeslaDeviceName, settings.TeslaBluetoothAddress, CancellationToken.None);
		}
		catch (Exception ex)
		{
			settingsStore.SetStatus(ex.Message);
		}

		RefreshStatus();
	}

	private async void OnTestEchoClicked(object? sender, EventArgs e)
	{
		try
		{
			var settings = ReadSettings();
			settingsStore.Save(settings);
			await automationCoordinator.BluetoothConnectedAsync(settings.EchoDeviceName, settings.EchoBluetoothAddress, CancellationToken.None);
		}
		catch (Exception ex)
		{
			settingsStore.SetStatus(ex.Message);
		}

		RefreshStatus();
	}

	private async void OnTestChargePointClicked(object? sender, EventArgs e)
	{
		try
		{
			var settings = ReadSettings();
			settingsStore.Save(settings);
			await automationCoordinator.OfficeEnteredAsync(CancellationToken.None);
		}
		catch (Exception ex)
		{
			settingsStore.SetStatus(ex.Message);
		}

		RefreshStatus();
	}

	private async void OnUseCurrentOfficeLocationClicked(object? sender, EventArgs e)
	{
		try
		{
			var granted = await permissionService.RequestAutomationPermissionsAsync();
			if (!granted)
			{
				settingsStore.SetStatus("Location permission is required to read the current office location.");
				RefreshStatus();
				return;
			}

			var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(15));
			var location = await Geolocation.Default.GetLocationAsync(request);
			if (location is null)
			{
				settingsStore.SetStatus("Android did not return a current location.");
				RefreshStatus();
				return;
			}

			OfficeLatitudeEntry.Text = location.Latitude.ToString(CultureInfo.InvariantCulture);
			OfficeLongitudeEntry.Text = location.Longitude.ToString(CultureInfo.InvariantCulture);
			if (string.IsNullOrWhiteSpace(OfficeRadiusEntry.Text))
			{
				OfficeRadiusEntry.Text = "120";
			}

			SaveWithoutRegisteringHome("Current office location captured. Tap Save to update the arrival monitor.");
		}
		catch (Exception ex)
		{
			settingsStore.SetStatus(ex.Message);
		}

		RefreshStatus();
	}

	private AppSettings ReadSettings()
	{
		return new AppSettings
		{
			TeslaDeviceName = TeslaDeviceNameEntry.Text ?? string.Empty,
			TeslaBluetoothAddress = TeslaBluetoothAddressEntry.Text ?? string.Empty,
			EchoDeviceName = EchoDeviceNameEntry.Text ?? string.Empty,
			EchoBluetoothAddress = EchoBluetoothAddressEntry.Text ?? string.Empty,
			IsHomeEnabled = HomeEnabledSwitch.IsToggled,
			MusicUrl = MusicUrlEntry.Text ?? string.Empty,
			OfficeLatitude = ReadDouble(OfficeLatitudeEntry.Text),
			OfficeLongitude = ReadDouble(OfficeLongitudeEntry.Text),
			OfficeRadiusMeters = Math.Max(25, ReadDouble(OfficeRadiusEntry.Text, 120)),
			IsChargePointReminderEnabled = ChargePointReminderEnabledSwitch.IsToggled,
			ChargePointStationName = ChargePointStationNameEntry.Text ?? string.Empty,
			IsChargePointAlertAutomationEnabled = ChargePointAlertAutomationEnabledSwitch.IsToggled,
			OpenAiNotesEndpoint = OpenAiNotesEndpointEntry.Text ?? string.Empty
		};
	}

	private static double ReadDouble(string? value, double defaultValue = 0)
	{
		return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
			? parsed
			: defaultValue;
	}

	private static string FormatCoordinate(double value)
	{
		return value == 0 ? string.Empty : value.ToString(CultureInfo.InvariantCulture);
	}

	private void RefreshStatus()
	{
		StatusLabel.Text = settingsStore.LastStatus;
	}

	private async Task<BluetoothDeviceOption?> PickPairedDeviceAsync(string title)
	{
		try
		{
			settingsStore.SetStatus("Reading paired Bluetooth devices.");
			RefreshStatus();

			var permissionsGranted = await permissionService.RequestAutomationPermissionsAsync();
			var devices = bluetoothDeviceStore.GetPaired();
			if (devices.Count == 0)
			{
				var message = permissionsGranted
					? "No paired Bluetooth devices were returned by Android. Pair Tesla and Echo in Android Bluetooth settings, then try again."
					: "Bluetooth permission is required. Allow Nearby devices/Bluetooth permission, then tap the picker again.";

				settingsStore.SetStatus(message);
				RefreshStatus();
				await DisplayAlertAsync("Bluetooth devices", message, "OK");
				return null;
			}

			var selected = await DisplayActionSheetAsync(
				title,
				"Cancel",
				null,
				devices.Select(device => device.DisplayName).ToArray());
			if (string.IsNullOrWhiteSpace(selected) || selected == "Cancel")
			{
				return null;
			}

			return devices.FirstOrDefault(device => device.DisplayName == selected);
		}
		catch (Exception ex)
		{
			settingsStore.SetStatus(ex.Message);
			RefreshStatus();
			await DisplayAlertAsync("Bluetooth devices", ex.Message, "OK");
			return null;
		}
	}

	private void SaveWithoutRegisteringHome(string status)
	{
		settingsStore.Save(ReadSettings());
		settingsStore.SetStatus(status);
		RefreshStatus();
	}
}
