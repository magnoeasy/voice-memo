using System.Text.Json;
using TeslaHomeMusic.Models;

namespace TeslaHomeMusic.Services;

public sealed class VoiceNoteStore : IVoiceNoteStore, IDisposable
{
	private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
	{
		WriteIndented = true
	};

	private readonly string path = Path.Combine(FileSystem.AppDataDirectory, "voice-notes.json");
	private readonly SemaphoreSlim fileGate = new(1, 1);

	public event EventHandler? NotesChanged;

	public async Task<IReadOnlyList<VoiceNote>> LoadAsync(CancellationToken cancellationToken)
	{
		await fileGate.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			return await LoadCoreAsync(cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			fileGate.Release();
		}
	}

	private async Task<IReadOnlyList<VoiceNote>> LoadCoreAsync(CancellationToken cancellationToken)
	{
		if (!File.Exists(path))
		{
			return [];
		}

		await using var stream = File.OpenRead(path);
		var notes = await JsonSerializer.DeserializeAsync<List<VoiceNote>>(stream, SerializerOptions, cancellationToken)
			.ConfigureAwait(false);

		return notes?
			.OrderByDescending(note => note.CreatedAt)
			.ToArray() ?? [];
	}

	public async Task SaveAsync(VoiceNote note, CancellationToken cancellationToken)
	{
		await fileGate.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			var notes = (await LoadCoreAsync(cancellationToken).ConfigureAwait(false)).ToList();
			var existingIndex = notes.FindIndex(existing => existing.Id == note.Id);
			if (existingIndex >= 0)
			{
				notes[existingIndex] = note;
			}
			else
			{
				notes.Insert(0, note);
			}

			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? FileSystem.AppDataDirectory);
			await using var stream = File.Create(path);
			await JsonSerializer.SerializeAsync(stream, notes, SerializerOptions, cancellationToken)
				.ConfigureAwait(false);
		}
		finally
		{
			fileGate.Release();
		}

		NotesChanged?.Invoke(this, EventArgs.Empty);
	}

	public async Task DeleteAllAsync(CancellationToken cancellationToken)
	{
		await fileGate.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			var notes = await LoadCoreAsync(cancellationToken).ConfigureAwait(false);
			foreach (var note in notes)
			{
				if (!string.IsNullOrWhiteSpace(note.AudioPath) && File.Exists(note.AudioPath))
				{
					File.Delete(note.AudioPath);
				}
			}

			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		finally
		{
			fileGate.Release();
		}

		NotesChanged?.Invoke(this, EventArgs.Empty);
	}

	public void Dispose()
	{
		fileGate.Dispose();
	}
}
