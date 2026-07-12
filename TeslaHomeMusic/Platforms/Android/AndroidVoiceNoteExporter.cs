using Android.Content;
using Android.OS;
using Android.Provider;
using TeslaHomeMusic.Models;
using TeslaHomeMusic.Services;
using SystemEnvironment = System.Environment;

namespace TeslaHomeMusic.Platforms.Android;

public sealed class AndroidVoiceNoteExporter : IVoiceNoteExporter
{
	#pragma warning disable CA1416
	public async Task<string> ExportAsync(VoiceNote note, CancellationToken cancellationToken)
	{
		var exported = await CreateFileAsync(note, cancellationToken).ConfigureAwait(false);
		return $"Downloads/{exported.FileName}";
	}

	public async Task<string> ExportAndShareAsync(VoiceNote note, CancellationToken cancellationToken)
	{
		var exported = await CreateFileAsync(note, cancellationToken).ConfigureAwait(false);
		ShareFile(exported.Uri, "Share transcript");
		return $"Downloads/{exported.FileName}";
	}

	public async Task<string> ExportAllAndShareAsync(IReadOnlyList<VoiceNote> notes, CancellationToken cancellationToken)
	{
		if (notes.Count == 0)
		{
			throw new InvalidOperationException("There are no saved notes to export.");
		}

		var fileName = $"voice-notes-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.txt";
		var content = string.Join(
			$"{SystemEnvironment.NewLine}{SystemEnvironment.NewLine}========================================{SystemEnvironment.NewLine}{SystemEnvironment.NewLine}",
			notes.OrderBy(note => note.CreatedAt).Select(CreateContent));
		var exported = await CreateFileAsync(fileName, content, cancellationToken).ConfigureAwait(false);
		ShareFile(exported.Uri, "Share all voice notes");
		return $"Downloads/{exported.FileName}";
	}

	private static async Task<(global::Android.Net.Uri Uri, string FileName)> CreateFileAsync(VoiceNote note, CancellationToken cancellationToken)
		=> await CreateFileAsync(CreateFileName(note), CreateContent(note), cancellationToken).ConfigureAwait(false);

	private static async Task<(global::Android.Net.Uri Uri, string FileName)> CreateFileAsync(string fileName, string content, CancellationToken cancellationToken)
	{
		if (Build.VERSION.SdkInt < BuildVersionCodes.Q)
		{
			throw new InvalidOperationException("Downloads export requires Android 10 or newer.");
		}

		var resolver = global::Android.App.Application.Context.ContentResolver
			?? throw new InvalidOperationException("Android storage is unavailable.");
		using var values = new ContentValues();
		values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
		values.Put(MediaStore.IMediaColumns.MimeType, "text/plain");
		values.Put(MediaStore.IMediaColumns.RelativePath, global::Android.OS.Environment.DirectoryDownloads);
		values.Put(MediaStore.IMediaColumns.IsPending, 1);

		var uri = resolver.Insert(MediaStore.Downloads.ExternalContentUri, values)
			?? throw new InvalidOperationException("Android could not create a file in Downloads.");

		try
		{
			await using var stream = resolver.OpenOutputStream(uri)
				?? throw new InvalidOperationException("Android could not open the Downloads file.");
			await using var writer = new StreamWriter(stream);
			await writer.WriteAsync(content.AsMemory(), cancellationToken).ConfigureAwait(false);
			await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

			using var publishedValues = new ContentValues();
			publishedValues.Put(MediaStore.IMediaColumns.IsPending, 0);
			resolver.Update(uri, publishedValues, null, null);
			return (uri, fileName);
		}
		catch
		{
			resolver.Delete(uri, null, null);
			throw;
		}
	}

	private static void ShareFile(global::Android.Net.Uri uri, string title)
	{
		var context = global::Android.App.Application.Context;
		var send = new Intent(Intent.ActionSend);
		send.SetType("text/plain");
		send.PutExtra(Intent.ExtraStream, uri);
		send.AddFlags(ActivityFlags.GrantReadUriPermission);
		context.StartActivity(Intent.CreateChooser(send, title)?.AddFlags(ActivityFlags.NewTask));
	}
	#pragma warning restore CA1416

	private static string CreateFileName(VoiceNote note)
	{
		var title = string.IsNullOrWhiteSpace(note.Title) ? "voice-note" : note.Title.Trim();
		var invalid = Path.GetInvalidFileNameChars();
		var safeTitle = string.Concat(title.Select(character => invalid.Contains(character) ? '_' : character));
		return $"{note.CreatedAt:yyyyMMdd-HHmmss}-{safeTitle}.txt";
	}

	private static string CreateContent(VoiceNote note)
	{
		var content = new List<string>
		{
			$"Title: {note.Title}",
			$"Created: {note.CreatedAt:yyyy-MM-dd HH:mm:ss zzz}",
			string.Empty,
			"Transcript:",
			note.RawText.Trim()
		};

		if (!string.IsNullOrWhiteSpace(note.CleanText))
		{
			content.Add(string.Empty);
			content.Add("Cleaned text:");
			content.Add(note.CleanText.Trim());
		}

		if (note.Actions.Count > 0)
		{
			content.Add(string.Empty);
			content.Add("Actions:");
			content.AddRange(note.Actions.Select(action => $"- {action}"));
		}

		return string.Join(SystemEnvironment.NewLine, content) + SystemEnvironment.NewLine;
	}
}
