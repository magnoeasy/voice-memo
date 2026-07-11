namespace TeslaHomeMusic.Models;

public sealed class BluetoothDeviceOption
{
	public required string Name { get; init; }

	public required string Address { get; init; }

	public string DisplayName => string.IsNullOrWhiteSpace(Address) ? Name : $"{Name} ({Address})";
}
