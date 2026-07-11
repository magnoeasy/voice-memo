namespace TeslaHomeMusic.Services;

public interface IAutomationCoordinator
{
	Task BluetoothConnectedAsync(string deviceName, string bluetoothAddress, CancellationToken cancellationToken);

	Task OfficeEnteredAsync(CancellationToken cancellationToken);
}
