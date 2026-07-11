using Android.Content;
using Android.OS;
using Android.Speech;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic.Platforms.Android;

public sealed class AndroidSpeechToTextService : ISpeechToTextService
{
	public async Task<string> ListenAsync(CancellationToken cancellationToken)
	{
		var permission = await Permissions.RequestAsync<Permissions.Microphone>();
		if (permission != PermissionStatus.Granted)
		{
			throw new InvalidOperationException("Microphone permission is required for dictation.");
		}

		var activity = Platform.CurrentActivity
			?? throw new InvalidOperationException("Android activity is not available.");

		if (!SpeechRecognizer.IsRecognitionAvailable(activity))
		{
			throw new InvalidOperationException("Android speech recognition is not available on this device.");
		}

		if (activity is not TeslaHomeMusic.MainActivity mainActivity)
		{
			throw new InvalidOperationException("VoxPad activity is not available for dictation.");
		}

		return await mainActivity.RecognizeSpeechAsync(CreateIntent(), cancellationToken).ConfigureAwait(false);
	}

	private static Intent CreateIntent()
	{
		var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
		intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
		intent.PutExtra(RecognizerIntent.ExtraPartialResults, false);
		intent.PutExtra(RecognizerIntent.ExtraPrompt, "Speak your note");
		intent.PutExtra(RecognizerIntent.ExtraSpeechInputMinimumLengthMillis, 10_000);
		intent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 10_000);
		intent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 10_000);
		if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
		{
			intent.PutExtra(RecognizerIntent.ExtraPreferOffline, false);
		}

		return intent;
	}
}
