using Microsoft.Extensions.Logging;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif
		builder.Services.AddSingleton<ISettingsStore, SettingsStore>();
		builder.Services.AddSingleton<IAutomationCoordinator, AutomationCoordinator>();
		builder.Services.AddSingleton<IVoiceNoteStore, VoiceNoteStore>();
		builder.Services.AddSingleton<IVoiceNoteInbox, VoiceNoteInbox>();
		builder.Services.AddSingleton<IOpenAiNoteClient, OpenAiNoteClient>();
		builder.Services.AddSingleton<HttpClient>();
#if ANDROID
		builder.Services.AddSingleton<IPermissionService, Platforms.Android.AndroidPermissionService>();
		builder.Services.AddSingleton<ISpeechToTextService, Platforms.Android.AndroidSpeechToTextService>();
#if AUTOMATION_APP
		builder.Services.AddSingleton<IBluetoothDeviceStore, Platforms.Android.AndroidBluetoothDeviceStore>();
		builder.Services.AddSingleton<IHomeArrivalMonitor, Platforms.Android.AndroidHomeArrivalMonitor>();
		builder.Services.AddSingleton<IPlaybackClient, Platforms.Android.YouTubeMusicPlaybackClient>();
		builder.Services.AddSingleton<IChargePointNotifier, Platforms.Android.AndroidChargePointNotifier>();
#else
		builder.Services.AddSingleton<IHandsFreeNoteRecorder, Platforms.Android.AndroidHandsFreeNoteRecorder>();
#endif
#endif

		return builder.Build();
	}
}
