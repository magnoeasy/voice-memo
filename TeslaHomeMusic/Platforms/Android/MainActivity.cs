using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic;

#if AUTOMATION_APP
[Activity(Name = "com.fernandoalves.teslahomemusic.MainActivity", Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, Exported = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
#else
[Activity(Name = "com.fernandoalves.voxpad.MainActivity", Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, Exported = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
#endif
[MetaData("android.app.shortcuts", Resource = "@xml/shortcuts")]
[IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "text/plain")]
[IntentFilter(["android.intent.action.PROCESS_TEXT"], Categories = [Intent.CategoryDefault], DataMimeType = "text/plain")]
[IntentFilter([Intent.ActionView], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataScheme = "voxpad", DataHost = "voice-note")]
public class MainActivity : MauiAppCompatActivity
{
	private const int SpeechRequestCode = 9401;
	private static readonly TimeSpan VoiceRequestDebounce = TimeSpan.FromSeconds(5);
	private static TaskCompletionSource<string>? speechCompletion;
	private static DateTimeOffset lastVoiceRequestAt = DateTimeOffset.MinValue;

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		ConfigureHandsFreeWindow();

		#if !AUTOMATION_APP
		var services = IPlatformApplication.Current?.Services;
		HandleVoiceNoteIntent(Intent);
		#endif
	}

	private void ConfigureHandsFreeWindow()
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(27))
		{
			SetShowWhenLocked(true);
			SetTurnScreenOn(true);
		}
		else
		{
			Window?.AddFlags(WindowManagerFlags.ShowWhenLocked | WindowManagerFlags.TurnScreenOn);
		}

		Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
	}

	protected override void OnNewIntent(Intent? intent)
	{
		base.OnNewIntent(intent);

		if (intent is not null)
		{
			Intent = intent;
			#if !AUTOMATION_APP
			HandleVoiceNoteIntent(intent);
			#endif
		}
	}

	public Task<string> RecognizeSpeechAsync(Intent intent, CancellationToken cancellationToken)
	{
		if (speechCompletion is not null)
		{
			return Task.FromException<string>(new InvalidOperationException("Speech recognition is already running."));
		}

		var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		speechCompletion = completion;
		var registration = cancellationToken.Register(() =>
		{
			speechCompletion?.TrySetCanceled(cancellationToken);
			speechCompletion = null;
		});

		try
		{
			StartActivityForResult(intent, SpeechRequestCode);
		}
		catch (Exception ex)
		{
			registration.Dispose();
			completion.TrySetException(ex);
			speechCompletion = null;
		}

		return CompleteWithRegistrationAsync(completion.Task, registration);
	}

	protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
	{
		base.OnActivityResult(requestCode, resultCode, data);

		if (requestCode != SpeechRequestCode || speechCompletion is null)
		{
			return;
		}

		var completion = speechCompletion;
		speechCompletion = null;

		if (resultCode != Result.Ok)
		{
			completion.TrySetResult(string.Empty);
			return;
		}

		var matches = data?.GetStringArrayListExtra(Android.Speech.RecognizerIntent.ExtraResults);
		completion.TrySetResult(matches?.FirstOrDefault() ?? string.Empty);
	}

	private static async Task<string> CompleteWithRegistrationAsync(Task<string> task, CancellationTokenRegistration registration)
	{
		try
		{
			return await task.ConfigureAwait(false);
		}
		finally
		{
			registration.Dispose();
		}
	}

	private static void HandleVoiceNoteIntent(Intent? intent)
	{
		if (!IsVoiceNoteIntent(intent))
		{
			return;
		}

		var now = DateTimeOffset.UtcNow;
		if (now - lastVoiceRequestAt < VoiceRequestDebounce)
		{
			return;
		}

		lastVoiceRequestAt = now;

		var services = IPlatformApplication.Current?.Services;
		var inbox = services?.GetService<IVoiceNoteInbox>();
		var text = ExtractText(intent);
		if (!string.IsNullOrWhiteSpace(text))
		{
			inbox?.Put(text);
		}
		else
		{
			inbox?.RequestDictation(autoSave: true);
		}

		MainThread.BeginInvokeOnMainThread(async () =>
		{
			if (Shell.Current is not null)
			{
				await Shell.Current.GoToAsync("//VoiceNotesPage");
			}
		});
	}

	private static bool IsVoiceNoteIntent(Intent? intent)
	{
		return intent?.Action is Intent.ActionSend
			or Intent.ActionView
			or "android.intent.action.PROCESS_TEXT"
			|| IsLauncherIntent(intent);
	}

	private static bool IsLauncherIntent(Intent? intent)
	{
		return intent?.Action == Intent.ActionMain
			&& intent.HasCategory(Intent.CategoryLauncher);
	}

	private static string ExtractText(Intent? intent)
	{
		if (intent is null)
		{
			return string.Empty;
		}

		var sharedText = intent.GetStringExtra(Intent.ExtraText);
		if (!string.IsNullOrWhiteSpace(sharedText))
		{
			return sharedText;
		}

		var processedText = intent.GetStringExtra("android.intent.extra.PROCESS_TEXT");
		if (!string.IsNullOrWhiteSpace(processedText))
		{
			return processedText;
		}

		return intent.Data?.GetQueryParameter("text")
			?? intent.Data?.GetQueryParameter("note")
			?? string.Empty;
	}
}
