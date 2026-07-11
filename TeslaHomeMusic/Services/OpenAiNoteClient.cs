using System.Net.Http.Json;
using TeslaHomeMusic.Models;

namespace TeslaHomeMusic.Services;

public sealed class OpenAiNoteClient(HttpClient httpClient, ISettingsStore settingsStore) : IOpenAiNoteClient
{
	public async Task<OpenAiNoteResponse> ShareAsync(VoiceNote note, CancellationToken cancellationToken)
	{
		var endpoint = settingsStore.Load().OpenAiNotesEndpoint;
		if (string.IsNullOrWhiteSpace(endpoint))
		{
			throw new InvalidOperationException("Set the OpenAI notes backend endpoint before sharing notes.");
		}

		var request = new OpenAiNoteRequest
		{
			RawText = note.RawText,
			CreatedAt = note.CreatedAt,
			Source = "android-voice"
		};

		using var response = await httpClient.PostAsJsonAsync(endpoint, request, cancellationToken)
			.ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		var result = await response.Content.ReadFromJsonAsync<OpenAiNoteResponse>(cancellationToken)
			.ConfigureAwait(false);

		return result ?? throw new InvalidOperationException("The OpenAI notes backend returned an empty response.");
	}

	public async Task<OpenAiNoteResponse> TranscribeAsync(VoiceNote note, CancellationToken cancellationToken)
	{
		var endpoint = settingsStore.Load().OpenAiNotesEndpoint;
		if (string.IsNullOrWhiteSpace(endpoint))
		{
			throw new InvalidOperationException("Set the OpenAI notes backend endpoint before transcribing audio notes.");
		}

		if (string.IsNullOrWhiteSpace(note.AudioPath) || !File.Exists(note.AudioPath))
		{
			throw new InvalidOperationException("The audio note file is not available for transcription.");
		}

		await using var audio = File.OpenRead(note.AudioPath);
		using var content = new MultipartFormDataContent
		{
			{ new StreamContent(audio), "audio", Path.GetFileName(note.AudioPath) },
			{ new StringContent(note.CreatedAt.ToString("O")), "createdAt" },
			{ new StringContent("android-voice-audio"), "source" }
		};

		using var response = await httpClient.PostAsync(endpoint, content, cancellationToken)
			.ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		var result = await response.Content.ReadFromJsonAsync<OpenAiNoteResponse>(cancellationToken)
			.ConfigureAwait(false);

		return result ?? throw new InvalidOperationException("The OpenAI notes backend returned an empty response.");
	}
}
