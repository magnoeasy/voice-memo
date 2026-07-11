using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Java.Lang;
using TeslaHomeMusic.Models;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic.Platforms.Android;

public sealed class AndroidHomeArrivalMonitor(ISettingsStore settingsStore) : IHomeArrivalMonitor
{
	private const int OfficeRequestCode = 6202;
	private const string ActionOfficeEntered = "com.fernandoalves.teslahomemusic.OFFICE_ENTERED";

	public void Apply(AppSettings settings)
	{
		var context = global::Android.App.Application.Context;
		if (Build.VERSION.SdkInt >= BuildVersionCodes.M &&
			context.CheckSelfPermission("android.permission.ACCESS_FINE_LOCATION") != Permission.Granted)
		{
			settingsStore.SetStatus("Location permission is required for arrival monitoring.");
			return;
		}

		var manager = (LocationManager?)context.GetSystemService(Context.LocationService);
		if (manager is null)
		{
			settingsStore.SetStatus("Location service is unavailable.");
			return;
		}

		try
		{
			ApplyAlert(
				context,
				manager,
				typeof(OfficeArrivalReceiver),
				ActionOfficeEntered,
				OfficeRequestCode,
				settings.IsChargePointReminderEnabled,
				settings.OfficeLatitude,
				settings.OfficeLongitude,
				settings.OfficeRadiusMeters);
		}
		catch (SecurityException)
		{
			settingsStore.SetStatus("Location permission is required for arrival monitoring.");
			return;
		}

		settingsStore.SetStatus("Office arrival monitor updated.");
	}

	private static void ApplyAlert(
		Context context,
		LocationManager manager,
		Type receiverType,
		string action,
		int requestCode,
		bool enabled,
		double latitude,
		double longitude,
		double radiusMeters)
	{
		var pendingIntent = CreatePendingIntent(context, receiverType, action, requestCode);
		if (pendingIntent is null)
		{
			return;
		}

		manager.RemoveProximityAlert(pendingIntent);
		if (!enabled || latitude == 0 || longitude == 0)
		{
			return;
		}

#pragma warning disable CA1422
		manager.AddProximityAlert(
			latitude,
			longitude,
			(float)radiusMeters,
			-1,
			pendingIntent);
#pragma warning restore CA1422
	}

	private static PendingIntent? CreatePendingIntent(
		Context context,
		Type receiverType,
		string action,
		int requestCode)
	{
		var intent = new Intent(context, receiverType);
		intent.SetAction(action);
		var flags = PendingIntentFlags.UpdateCurrent;
		if (OperatingSystem.IsAndroidVersionAtLeast(31))
		{
			flags |= PendingIntentFlags.Mutable;
		}

		return PendingIntent.GetBroadcast(
			context,
			requestCode,
			intent,
			flags);
	}
}
