using System.Net.Http.Headers;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("openai", client =>
{
	client.BaseAddress = new Uri("https://api.openai.com/");
	client.Timeout = TimeSpan.FromMinutes(5);
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", configured = HasApiKey() }));

app.MapPost("/voice-notes", async (HttpRequest request, IHttpClientFactory clients, CancellationToken cancellationToken) =>
{
	if (!HasApiKey())
	{
		return Results.Problem("OPENAI_API_KEY is not configured on the computer running the transcription service.", statusCode: StatusCodes.Status503ServiceUnavailable);
	}

	if (!request.HasFormContentType)
	{
		return Results.BadRequest("Expected multipart form data with an audio file.");
	}

	var form = await request.ReadFormAsync(cancellationToken);
	var audio = form.Files.GetFile("audio");
	if (audio is null || audio.Length == 0)
	{
		return Results.BadRequest("The audio form field is required.");
	}

	using var content = new MultipartFormDataContent();
	await using var audioStream = audio.OpenReadStream();
	using var audioContent = new StreamContent(audioStream);
	audioContent.Headers.ContentType = new MediaTypeHeaderValue(audio.ContentType ?? "audio/wav");
	content.Add(audioContent, "file", audio.FileName);
	content.Add(new StringContent("gpt-4o-transcribe"), "model");

	var client = clients.CreateClient("openai");
	using var openAiRequest = new HttpRequestMessage(HttpMethod.Post, "v1/audio/transcriptions")
	{
		Content = content
	};
	openAiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

	using var response = await client.SendAsync(openAiRequest, cancellationToken);
	var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
	if (!response.IsSuccessStatusCode)
	{
		return Results.Problem($"OpenAI transcription failed ({(int)response.StatusCode}): {responseBody}", statusCode: StatusCodes.Status502BadGateway);
	}

	using var json = JsonDocument.Parse(responseBody);
	var text = json.RootElement.TryGetProperty("text", out var textElement)
		? textElement.GetString()?.Trim() ?? string.Empty
		: string.Empty;
	if (string.IsNullOrWhiteSpace(text))
	{
		return Results.Problem("OpenAI returned an empty transcription.", statusCode: StatusCodes.Status502BadGateway);
	}

	return Results.Ok(new
	{
		title = CreateTitle(text),
		cleanText = text,
		actions = Array.Empty<string>()
	});
});

app.Run("http://0.0.0.0:5080");

static bool HasApiKey() => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

static string CreateTitle(string text)
{
	var singleLine = text.ReplaceLineEndings(" ").Trim();
	return singleLine.Length <= 48 ? singleLine : string.Concat(singleLine.AsSpan(0, 45), "...");
}
