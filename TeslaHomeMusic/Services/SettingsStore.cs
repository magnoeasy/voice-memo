using TeslaHomeMusic.Models;

namespace TeslaHomeMusic.Services;

public sealed class SettingsStore : ISettingsStore
{
	private const string TeslaDeviceNameKey = "tesla_device_name";
	private const string TeslaBluetoothAddressKey = "tesla_bluetooth_address";
	private const string EchoDeviceNameKey = "echo_device_name";
	private const string EchoBluetoothAddressKey = "echo_bluetooth_address";
	private const string MusicUrlKey = "music_url";
	private const string IsHomeEnabledKey = "is_home_enabled";
	private const string OfficeLatitudeKey = "office_latitude";
	private const string OfficeLongitudeKey = "office_longitude";
	private const string OfficeRadiusMetersKey = "office_radius_meters";
	private const string IsChargePointReminderEnabledKey = "is_chargepoint_reminder_enabled";
	private const string ChargePointStationNameKey = "chargepoint_station_name";
	private const string IsChargePointAlertAutomationEnabledKey = "is_chargepoint_alert_automation_enabled";
	private const string OpenAiNotesEndpointKey = "openai_notes_endpoint";
	private const string LastStatusKey = "last_status";

	public string LastStatus => Preferences.Default.Get(LastStatusKey, "Waiting for setup.");

	public AppSettings Load()
	{
		return new AppSettings
		{
			TeslaDeviceName = Preferences.Default.Get(TeslaDeviceNameKey, "Tesla"),
			TeslaBluetoothAddress = Preferences.Default.Get(TeslaBluetoothAddressKey, string.Empty),
			EchoDeviceName = Preferences.Default.Get(EchoDeviceNameKey, "Echo"),
			EchoBluetoothAddress = Preferences.Default.Get(EchoBluetoothAddressKey, string.Empty),
			MusicUrl = Preferences.Default.Get(MusicUrlKey, string.Empty),
			IsHomeEnabled = Preferences.Default.Get(IsHomeEnabledKey, false),
			OfficeLatitude = Preferences.Default.Get(OfficeLatitudeKey, 0d),
			OfficeLongitude = Preferences.Default.Get(OfficeLongitudeKey, 0d),
			OfficeRadiusMeters = Preferences.Default.Get(OfficeRadiusMetersKey, 120d),
			IsChargePointReminderEnabled = Preferences.Default.Get(IsChargePointReminderEnabledKey, false),
			ChargePointStationName = Preferences.Default.Get(ChargePointStationNameKey, "HPI HOUSTON EV STATION"),
			IsChargePointAlertAutomationEnabled = Preferences.Default.Get(IsChargePointAlertAutomationEnabledKey, false),
			OpenAiNotesEndpoint = Preferences.Default.Get(OpenAiNotesEndpointKey, string.Empty)
		};
	}

	public void Save(AppSettings settings)
	{
		Preferences.Default.Set(TeslaDeviceNameKey, settings.TeslaDeviceName.Trim());
		Preferences.Default.Set(TeslaBluetoothAddressKey, settings.TeslaBluetoothAddress.Trim());
		Preferences.Default.Set(EchoDeviceNameKey, settings.EchoDeviceName.Trim());
		Preferences.Default.Set(EchoBluetoothAddressKey, settings.EchoBluetoothAddress.Trim());
		Preferences.Default.Set(MusicUrlKey, settings.MusicUrl.Trim());
		Preferences.Default.Set(IsHomeEnabledKey, settings.IsHomeEnabled);
		Preferences.Default.Set(OfficeLatitudeKey, settings.OfficeLatitude);
		Preferences.Default.Set(OfficeLongitudeKey, settings.OfficeLongitude);
		Preferences.Default.Set(OfficeRadiusMetersKey, settings.OfficeRadiusMeters);
		Preferences.Default.Set(IsChargePointReminderEnabledKey, settings.IsChargePointReminderEnabled);
		Preferences.Default.Set(ChargePointStationNameKey, settings.ChargePointStationName.Trim());
		Preferences.Default.Set(IsChargePointAlertAutomationEnabledKey, settings.IsChargePointAlertAutomationEnabled);
		Preferences.Default.Set(OpenAiNotesEndpointKey, settings.OpenAiNotesEndpoint.Trim());
	}

	public void SetStatus(string status)
	{
		Preferences.Default.Set(LastStatusKey, $"{DateTimeOffset.Now:g} - {status}");
	}
}
