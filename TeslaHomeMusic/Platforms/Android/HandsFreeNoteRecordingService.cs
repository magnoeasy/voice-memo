using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using TeslaHomeMusic.Models;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic.Platforms.Android;

[Service(Exported = false, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeMicrophone)]
public sealed class HandsFreeNoteRecordingService : Service
{
	private const int NotificationId = 9051;
	private const string ChannelId = "hands_free_notes";
	private const int SampleRate = 16_000;
	private const int SilenceSeconds = 10;
	private const int MaxRecordingSeconds = 300;
	private const double SilenceThreshold = 700;
	private CancellationTokenSource? recording;

	public override IBinder? OnBind(Intent? intent)
	{
		return null;
	}

	public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
	{
		if (recording is not null)
		{
			return StartCommandResult.NotSticky;
		}

		CreateNotificationChannel();
		StartForeground(NotificationId, CreateNotification("Recording voice note"));
		recording = new CancellationTokenSource();
		_ = RecordAsync(recording.Token);
		return StartCommandResult.NotSticky;
	}

	public override void OnDestroy()
	{
		recording?.Cancel();
		recording?.Dispose();
		recording = null;
		base.OnDestroy();
	}

	private async Task RecordAsync(CancellationToken cancellationToken)
	{
		try
		{
			var path = CreateAudioPath();
			await RecordWavAsync(path, cancellationToken).ConfigureAwait(false);
			await SaveNoteAsync(path, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			SetStatus($"Hands-free recording failed: {ex.Message}");
		}
		finally
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(24))
			{
				StopForeground(StopForegroundFlags.Remove);
			}
			else
			{
				StopForeground(removeNotification: true);
			}

			StopSelf();
		}
	}

	private static async Task RecordWavAsync(string path, CancellationToken cancellationToken)
	{
		var minBuffer = AudioRecord.GetMinBufferSize(SampleRate, ChannelIn.Mono, Encoding.Pcm16bit);
		var bufferSize = Math.Max(minBuffer, SampleRate);
		using var recorder = new AudioRecord(AudioSource.Mic, SampleRate, ChannelIn.Mono, Encoding.Pcm16bit, bufferSize);
		if (recorder.State != State.Initialized)
		{
			throw new InvalidOperationException("Android could not initialize microphone recording.");
		}

		Directory.CreateDirectory(Path.GetDirectoryName(path) ?? FileSystem.AppDataDirectory);
		await using var stream = File.Create(path);
		WriteWavHeader(stream, 0);

		var buffer = new byte[bufferSize];
		var totalBytes = 0;
		var silentBytes = 0;
		var maxBytes = SampleRate * 2 * MaxRecordingSeconds;
		var silenceLimitBytes = SampleRate * 2 * SilenceSeconds;

		recorder.StartRecording();
		try
		{
			while (!cancellationToken.IsCancellationRequested && totalBytes < maxBytes)
			{
				var read = recorder.Read(buffer, 0, buffer.Length);
				if (read <= 0)
				{
					continue;
				}

				await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
				totalBytes += read;

				silentBytes = IsSilent(buffer, read) ? silentBytes + read : 0;
				if (totalBytes > SampleRate * 2 && silentBytes >= silenceLimitBytes)
				{
					break;
				}
			}
		}
		finally
		{
			recorder.Stop();
		}

		stream.Seek(0, SeekOrigin.Begin);
		WriteWavHeader(stream, totalBytes);
	}

	private static bool IsSilent(byte[] buffer, int count)
	{
		long total = 0;
		var samples = 0;
		for (var i = 0; i + 1 < count; i += 2)
		{
			var sample = BitConverter.ToInt16(buffer, i);
			total += Math.Abs(sample);
			samples++;
		}

		return samples == 0 || total / (double)samples < SilenceThreshold;
	}

	private static async Task SaveNoteAsync(string path, CancellationToken cancellationToken)
	{
		var services = IPlatformApplication.Current?.Services;
		var store = services?.GetRequiredService<IVoiceNoteStore>();
		var settings = services?.GetRequiredService<ISettingsStore>();
		var client = services?.GetService<IOpenAiNoteClient>();
		if (store is null || settings is null)
		{
			throw new InvalidOperationException("Application services are not available.");
		}

		var createdAt = DateTimeOffset.Now;
		var note = new VoiceNote
		{
			CreatedAt = createdAt,
			Title = $"Audio note {createdAt:g}",
			RawText = "Audio note saved for transcription.",
			AudioPath = path
		};
		await store.SaveAsync(note, cancellationToken).ConfigureAwait(false);
		settings.SetStatus("Hands-free audio note saved.");

		if (client is null || string.IsNullOrWhiteSpace(settings.Load().OpenAiNotesEndpoint))
		{
			settings.SetStatus("Hands-free audio note saved. Set the OpenAI notes backend endpoint to transcribe it.");
			return;
		}

		try
		{
			settings.SetStatus("Transcribing hands-free audio note.");
			var response = await client.TranscribeAsync(note, cancellationToken).ConfigureAwait(false);
			note.Title = string.IsNullOrWhiteSpace(response.Title) ? note.Title : response.Title.Trim();
			note.CleanText = response.CleanText.Trim();
			note.RawText = string.IsNullOrWhiteSpace(response.CleanText) ? note.RawText : response.CleanText.Trim();
			note.Actions = response.Actions;
			note.IsShared = true;
			await store.SaveAsync(note, cancellationToken).ConfigureAwait(false);
			settings.SetStatus("Hands-free audio note transcribed.");
		}
		catch (Exception ex)
		{
			settings.SetStatus($"Audio note saved, but transcription failed: {ex.Message}");
		}
	}

	private static string CreateAudioPath()
	{
		var fileName = $"voice-note-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.wav";
		return Path.Combine(FileSystem.AppDataDirectory, "audio-notes", fileName);
	}

	private void CreateNotificationChannel()
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			return;
		}

		var channel = new NotificationChannel(ChannelId, "Hands-free notes", NotificationImportance.Low)
		{
			Description = "Records hands-free voice notes."
		};
		var manager = GetSystemService(NotificationService) as NotificationManager;
		manager?.CreateNotificationChannel(channel);
	}

	private Notification CreateNotification(string text)
	{
		var intent = new Intent(this, typeof(TeslaHomeMusic.MainActivity));
		var pendingIntent = PendingIntent.GetActivity(
			this,
			0,
			intent,
			PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)
			?? throw new InvalidOperationException("Android did not create a notification launch intent.");

		var builder = new NotificationCompat.Builder(this, ChannelId);
		builder.SetSmallIcon(Resource.Mipmap.appicon);
		builder.SetContentTitle("VoxPad");
		builder.SetContentText(text);
		builder.SetOngoing(true);
		builder.SetContentIntent(pendingIntent);

		return builder.Build()
			?? throw new InvalidOperationException("Android did not create a recording notification.");
	}

	private static void WriteWavHeader(System.IO.Stream stream, int dataLength)
	{
		var byteRate = SampleRate * 2;
		using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
		writer.Write("RIFF"u8.ToArray());
		writer.Write(36 + dataLength);
		writer.Write("WAVE"u8.ToArray());
		writer.Write("fmt "u8.ToArray());
		writer.Write(16);
		writer.Write((short)1);
		writer.Write((short)1);
		writer.Write(SampleRate);
		writer.Write(byteRate);
		writer.Write((short)2);
		writer.Write((short)16);
		writer.Write("data"u8.ToArray());
		writer.Write(dataLength);
	}

	private static void SetStatus(string status)
	{
		var settings = IPlatformApplication.Current?.Services?.GetService<ISettingsStore>();
		settings?.SetStatus(status);
	}
}
