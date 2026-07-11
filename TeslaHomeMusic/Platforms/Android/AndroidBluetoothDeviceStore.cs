using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Java.Lang;
using TeslaHomeMusic.Models;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic.Platforms.Android;

public sealed class AndroidBluetoothDeviceStore : IBluetoothDeviceStore
{
	public IReadOnlyList<BluetoothDeviceOption> GetPaired()
	{
		var context = global::Android.App.Application.Context;
		if (Build.VERSION.SdkInt >= BuildVersionCodes.S &&
			context.CheckSelfPermission("android.permission.BLUETOOTH_CONNECT") != Permission.Granted)
		{
			return [];
		}

		var manager = (BluetoothManager?)context.GetSystemService(Context.BluetoothService);
		var adapter = manager?.Adapter;
		if (adapter?.BondedDevices is null)
		{
			return [];
		}

		var devices = new List<BluetoothDeviceOption>();
		foreach (var device in adapter.BondedDevices)
		{
			var name = ReadName(device);
			devices.Add(new BluetoothDeviceOption
			{
				Name = string.IsNullOrWhiteSpace(name) ? "Unknown" : name,
				Address = device.Address ?? string.Empty
			});
		}

		return devices
			.OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
			.ThenBy(device => device.Address, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	private static string ReadName(BluetoothDevice device)
	{
		try
		{
			return device.Name ?? string.Empty;
		}
		catch (SecurityException)
		{
			return string.Empty;
		}
	}
}
