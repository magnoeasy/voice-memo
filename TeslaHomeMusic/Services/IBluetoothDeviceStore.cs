using TeslaHomeMusic.Models;

namespace TeslaHomeMusic.Services;

public interface IBluetoothDeviceStore
{
	IReadOnlyList<BluetoothDeviceOption> GetPaired();
}
