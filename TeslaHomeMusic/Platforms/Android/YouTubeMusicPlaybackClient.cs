using Android.Content;
using Android.Media;
using Android.Views;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic.Platforms.Android;

public sealed class YouTubeMusicPlaybackClient : IPlaybackClient
{
	private const string PackageName = "com.google.android.apps.youtube.music";

	public async Task PlayAsync(string musicUrl, CancellationToken cancellationToken)
	{
		var context = global::Android.App.Application.Context;
		await WaitForBluetoothMediaRouteAsync(context, cancellationToken).ConfigureAwait(false);

		var intent = CreateIntent(context, musicUrl);
		if (intent is null)
		{
			throw new InvalidOperationException("Android could not open YouTube Music. Verify YouTube Music is installed and enabled for this Android profile.");
		}

		try
		{
			context.StartActivity(intent);
		}
		catch (global::Android.App.ForegroundServiceStartNotAllowedException)
		{
			throw new InvalidOperationException("Android blocked YouTube Music from starting in the background. Open Tesla Home Music once, then reconnect the Echo.");
		}
		catch (global::Android.Content.ActivityNotFoundException)
		{
			throw new InvalidOperationException("YouTube Music is not available on this Android profile.");
		}

		await Task.Delay(TimeSpan.FromMilliseconds(1500), cancellationToken).ConfigureAwait(false);
		SendPlay(context);
	}

	private static async Task WaitForBluetoothMediaRouteAsync(Context context, CancellationToken cancellationToken)
	{
		var audio = (AudioManager?)context.GetSystemService(Context.AudioService);
		if (audio is null)
		{
			return;
		}

		for (var attempt = 0; attempt < 5; attempt++)
		{
			if ((audio.GetDevices(GetDevicesTargets.Outputs) ?? [])
				.Any(device => device.Type == AudioDeviceType.BluetoothA2dp))
			{
				return;
			}

			await Task.Delay(TimeSpan.FromMilliseconds(700), cancellationToken).ConfigureAwait(false);
		}
	}

	private static Intent? CreateIntent(Context context, string musicUrl)
	{
		if (Uri.TryCreate(musicUrl, UriKind.Absolute, out var uri))
		{
			var view = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(uri.ToString()));
			view.SetPackage(PackageName);
			view.AddFlags(ActivityFlags.NewTask);
			return view;
		}

		var launch = context.PackageManager?.GetLaunchIntentForPackage(PackageName);
		if (launch is null)
		{
			launch = new Intent()
				.SetClassName(PackageName, $"{PackageName}.activities.MusicActivity")
				.AddFlags(ActivityFlags.NewTask);
		}

		launch?.AddFlags(ActivityFlags.NewTask);
		return launch;
	}

	private static void SendPlay(Context context)
	{
		var audio = (AudioManager?)context.GetSystemService(Context.AudioService);
		if (audio is null)
		{
			return;
		}

		var now = Java.Lang.JavaSystem.CurrentTimeMillis();
		audio.DispatchMediaKeyEvent(new KeyEvent(now, now, KeyEventActions.Down, Keycode.MediaPlay, 0));
		audio.DispatchMediaKeyEvent(new KeyEvent(now, now, KeyEventActions.Up, Keycode.MediaPlay, 0));
	}
}
