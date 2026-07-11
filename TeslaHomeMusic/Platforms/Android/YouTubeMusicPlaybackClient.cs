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
		var intent = CreateIntent(context, musicUrl);
		if (intent is null)
		{
			throw new InvalidOperationException("Android could not open YouTube Music. Verify YouTube Music is installed and enabled for this Android profile.");
		}

		context.StartActivity(intent);
		await Task.Delay(TimeSpan.FromMilliseconds(900), cancellationToken).ConfigureAwait(false);
		SendPlay(context);
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
