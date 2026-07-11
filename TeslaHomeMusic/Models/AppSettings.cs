namespace TeslaHomeMusic.Models;

public sealed class AppSettings
{
	public string TeslaDeviceName { get; set; } = "Tesla";

	public string TeslaBluetoothAddress { get; set; } = string.Empty;

	public string EchoDeviceName { get; set; } = "Echo";

	public string EchoBluetoothAddress { get; set; } = string.Empty;

	public string MusicUrl { get; set; } = string.Empty;

	public bool IsHomeEnabled { get; set; }

	public double OfficeLatitude { get; set; }

	public double OfficeLongitude { get; set; }

	public double OfficeRadiusMeters { get; set; } = 120;

	public bool IsChargePointReminderEnabled { get; set; }

	public string ChargePointStationName { get; set; } = "HPI HOUSTON EV STATION";

	public bool IsChargePointAlertAutomationEnabled { get; set; }

	public string OpenAiNotesEndpoint { get; set; } = string.Empty;
}
