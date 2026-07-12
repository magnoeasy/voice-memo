using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using TeslaHomeMusic.Models;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic;

public partial class VoiceNotesPage : ContentPage
{
	private readonly IVoiceNoteStore noteStore;
	private readonly IOpenAiNoteClient noteClient;
	private readonly ISettingsStore settingsStore;
	private readonly IVoiceNoteInbox noteInbox;
	private readonly IVoiceNoteExporter noteExporter;
	private readonly ISpeechToTextService? speechToText;
	private bool isHandlingVoiceRequest;

	public VoiceNotesPage()
	{
		InitializeComponent();

		var services = IPlatformApplication.Current?.Services
			?? throw new InvalidOperationException("Application services are not available.");

		noteStore = services.GetRequiredService<IVoiceNoteStore>();
		noteClient = services.GetRequiredService<IOpenAiNoteClient>();
		settingsStore = services.GetRequiredService<ISettingsStore>();
		noteInbox = services.GetRequiredService<IVoiceNoteInbox>();
		noteExporter = services.GetRequiredService<IVoiceNoteExporter>();
		speechToText = services.GetService<ISpeechToTextService>();
		noteInbox.VoiceRequested += OnVoiceRequested;
		noteStore.NotesChanged += OnNotesChanged;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		var pendingText = noteInbox.Take();
		EndpointEditor.Text = settingsStore.Load().OpenAiNotesEndpoint;
		if (!string.IsNullOrWhiteSpace(pendingText))
		{
			DraftEditor.Text = pendingText;
			settingsStore.SetStatus("Voice note text received.");
		}
		else if (noteInbox.TakeDictationRequest(out var autoSave))
		{
			if (autoSave)
			{
				await DictateAsync();
				if (!string.IsNullOrWhiteSpace(DraftEditor.Text))
				{
					await SaveDraftAsync(share: false);
				}

				await RefreshAsync();
				return;
			}

			await DictateAsync();
		}

		await RefreshAsync();
	}

	private async void OnSaveClicked(object? sender, EventArgs e) => await SaveDraftAsync(share: false);

	private async void OnSendDraftClicked(object? sender, EventArgs e) => await SaveDraftAsync(share: true);

	private async void OnDeleteAllNotesClicked(object? sender, EventArgs e)
	{
		var confirmed = await DisplayAlertAsync(
			"Permanently delete all saved notes?",
			"This cannot be undone. It removes every saved note and any old audio files attached to those notes.",
			"Delete all",
			"Cancel");
		if (!confirmed)
		{
			return;
		}

		await noteStore.DeleteAllAsync(CancellationToken.None);
		settingsStore.SetStatus("All saved notes deleted.");
		await RefreshAsync();
	}

	private async void OnDictateClicked(object? sender, EventArgs e)
	{
		await DictateAsync();
		await RefreshAsync();
	}

	private async void OnSaveEndpointClicked(object? sender, EventArgs e)
	{
		var settings = settingsStore.Load();
		settings.OpenAiNotesEndpoint = EndpointEditor.Text?.Trim() ?? string.Empty;
		settingsStore.Save(settings);
		settingsStore.SetStatus(string.IsNullOrWhiteSpace(settings.OpenAiNotesEndpoint)
			? "OpenAI notes backend endpoint cleared."
			: "OpenAI notes backend endpoint saved.");
		await RefreshAsync();
	}

	private void OnNotesChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());

	private async void OnVoiceRequested(object? sender, VoiceNoteRequestEventArgs e)
	{
		if (isHandlingVoiceRequest)
		{
			return;
		}

		isHandlingVoiceRequest = true;
		try
		{
			if (!string.IsNullOrWhiteSpace(e.Text))
			{
				noteInbox.Take();
				DraftEditor.Text = e.Text;
				settingsStore.SetStatus("Voice note text received.");
				await RefreshAsync();
				return;
			}

			if (e.ShouldDictate)
			{
				noteInbox.TakeDictationRequest(out _);
				if (e.AutoSave)
				{
					await DictateAsync();
					if (!string.IsNullOrWhiteSpace(DraftEditor.Text))
					{
						await SaveDraftAsync(share: false);
					}
				}
				else
				{
					await DictateAsync();
				}

				await RefreshAsync();
			}
		}
		finally
		{
			isHandlingVoiceRequest = false;
		}
	}

	private async void OnNoteSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.Count == 0 || e.CurrentSelection[0] is not DisplayNote displayNote)
		{
			return;
		}

		NotesCollection.SelectedItem = null;
		await ShareAsync(displayNote.Note);
	}

	private async void OnExportNoteClicked(object? sender, EventArgs e)
	{
		if (sender is not Button { BindingContext: DisplayNote displayNote })
		{
			return;
		}

		try
		{
			var location = await noteExporter.ExportAsync(displayNote.Note, CancellationToken.None);
			settingsStore.SetStatus($"Transcript exported to {location}.");
		}
		catch (Exception ex)
		{
			settingsStore.SetStatus(ex.Message);
		}

		await RefreshAsync();
	}

	private async void OnExportAndShareNoteClicked(object? sender, EventArgs e)
	{
		if (sender is not Button { BindingContext: DisplayNote displayNote })
		{
			return;
		}

		try
		{
			var location = await noteExporter.ExportAndShareAsync(displayNote.Note, CancellationToken.None);
			settingsStore.SetStatus($"Transcript exported and ready to share from {location}.");
		}
		catch (Exception ex)
		{
			settingsStore.SetStatus(ex.Message);
		}

		await RefreshAsync();
	}

	private async void OnExportAllAndShareClicked(object? sender, EventArgs e)
	{
		try
		{
			var notes = await noteStore.LoadAsync(CancellationToken.None);
			var location = await noteExporter.ExportAllAndShareAsync(notes, CancellationToken.None);
			settingsStore.SetStatus($"All notes exported and ready to share from {location}.");
		}
		catch (Exception ex)
		{
			settingsStore.SetStatus(ex.Message);
		}

		await RefreshAsync();
	}

	private async Task SaveDraftAsync(bool share)
	{
		var rawText = DraftEditor.Text?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(rawText))
		{
			settingsStore.SetStatus("Enter or dictate a note first.");
			await RefreshAsync();
			return;
		}

		var note = new VoiceNote
		{
			CreatedAt = DateTimeOffset.Now,
			RawText = rawText,
			Title = CreateTitle(rawText)
		};

		await noteStore.SaveAsync(note, CancellationToken.None);
		DraftEditor.Text = string.Empty;
		settingsStore.SetStatus("Voice note saved locally.");

		if (share)
		{
			await ShareAsync(note);
			return;
		}

		await RefreshAsync();
	}

	private async Task ShareAsync(VoiceNote note)
	{
		try
		{
			settingsStore.SetStatus("Sending voice note to OpenAI notes backend.");
			RefreshStatus();

			var response = await noteClient.ShareAsync(note, CancellationToken.None);
			note.Title = string.IsNullOrWhiteSpace(response.Title) ? note.Title : response.Title.Trim();
			note.CleanText = response.CleanText.Trim();
			note.Actions = response.Actions;
			note.IsShared = true;
			await noteStore.SaveAsync(note, CancellationToken.None);

			settingsStore.SetStatus("Voice note shared and updated.");
		}
		catch (Exception ex)
		{
			settingsStore.SetStatus(ex.Message);
		}

		await RefreshAsync();
	}

	private async Task RefreshAsync()
	{
		var notes = await noteStore.LoadAsync(CancellationToken.None);
		NotesCollection.ItemsSource = notes.Select(DisplayNote.From).ToArray();
		RefreshStatus();
	}

	private void RefreshStatus() => StatusLabel.Text = settingsStore.LastStatus;

	private async Task DictateAsync()
	{
		if (speechToText is null)
		{
			settingsStore.SetStatus("Speech recognition is not available on this platform.");
			return;
		}

		try
		{
			settingsStore.SetStatus("Listening for voice note.");
			RefreshStatus();

			var text = await speechToText.ListenAsync(CancellationToken.None);
			if (string.IsNullOrWhiteSpace(text))
			{
				settingsStore.SetStatus("No speech was recognized.");
				return;
			}

			DraftEditor.Text = string.IsNullOrWhiteSpace(DraftEditor.Text)
				? text
				: $"{DraftEditor.Text.Trim()} {text}";
			settingsStore.SetStatus("Dictation added to draft.");
		}
		catch (Exception ex)
		{
			settingsStore.SetStatus(ex.Message);
		}
	}

	private static string CreateTitle(string text)
	{
		var singleLine = text.ReplaceLineEndings(" ").Trim();
		return singleLine.Length <= 48 ? singleLine : string.Concat(singleLine.AsSpan(0, 45), "...");
	}
}
