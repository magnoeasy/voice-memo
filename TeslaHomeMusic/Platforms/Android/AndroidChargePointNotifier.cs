using Android.App;
using Android.Content;
using Android.OS;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic.Platforms.Android;

public sealed class AndroidChargePointNotifier : IChargePointNotifier
{
	private const string ChannelId = "chargepoint_reminders";
	private const int NotificationId = 7101;
	private const string ChargePointPackageName = "com.coulombtech";

	public void NotifyAvailableCheck()
	{
		var context = global::Android.App.Application.Context;
		var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
		if (manager is null)
		{
			return;
		}

		EnsureChannel(manager);

		var intent = context.PackageManager?.GetLaunchIntentForPackage(ChargePointPackageName);
		intent?.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);

		var flags = PendingIntentFlags.UpdateCurrent;
		if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
		{
			flags |= PendingIntentFlags.Immutable;
		}

		var pendingIntent = intent is null
			? null
			: PendingIntent.GetActivity(context, NotificationId, intent, flags);
		var notification = CreateNotification(context, pendingIntent);
		manager.Notify(NotificationId, notification);

		if (intent is not null)
		{
			try
			{
				context.StartActivity(intent);
			}
			catch (ActivityNotFoundException)
			{
				// The notification remains available if ChargePoint is removed between resolution and launch.
			}
		}
	}

	private static void EnsureChannel(NotificationManager manager)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			return;
		}

		var channel = new NotificationChannel(
			ChannelId,
			"ChargePoint reminders",
			NotificationImportance.Default)
		{
			Description = "Office arrival reminders to check ChargePoint availability."
		};
		manager.CreateNotificationChannel(channel);
	}

	private static Notification CreateNotification(Context context, PendingIntent? pendingIntent)
	{
		Notification.Builder builder;
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			builder = new Notification.Builder(context, ChannelId);
		}
		else
		{
#pragma warning disable CA1422
			builder = new Notification.Builder(context);
#pragma warning restore CA1422
		}

		builder
			.SetSmallIcon(Resource.Mipmap.appicon)
			.SetContentTitle("Check ChargePoint")
			.SetContentText("You arrived at the office. Check whether the charge station is available.")
			.SetAutoCancel(true)
			.SetShowWhen(true);

		if (pendingIntent is not null)
		{
			builder.SetContentIntent(pendingIntent);
		}

		return builder.Build();
	}
}
