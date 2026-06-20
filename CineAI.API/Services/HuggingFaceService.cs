using System.Text;
using System.Text.Json;

namespace CineAI.API.Services;

public class HuggingFaceService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public HuggingFaceService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["HuggingFace:ApiKey"] ?? "";
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<(string Label, double Confidence)> ClassifyAsync(string text)
    {
        var url = "https://openrouter.ai/api/v1/chat/completions";

        var payload = new
        {
            model = "nex-agi/nex-n2-pro:free",
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are a movie genre classifier. Given a movie description, respond with ONLY a JSON object in this exact format: {\"label\": \"Genre\", \"confidence\": 0.95}. Choose one genre from: Science Fiction, Action, Drama, Comedy, Horror, Romance, Thriller, Animation, Documentary. Nothing else, just the JSON."
                },
                new
                {
                    role = "user",
                    content = text
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Логируем ответ для отладки
        Console.WriteLine("OpenRouter response: " + responseBody);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        // Проверяем на ошибку от OpenRouter
        if (root.TryGetProperty("error", out var error))
        {
            var errorMsg = error.GetProperty("message").GetString();
            throw new Exception($"OpenRouter error: {errorMsg}");
        }

        var messageContent = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        Console.WriteLine("AI content: " + messageContent);

        // Убираем markdown теги если есть
        messageContent = messageContent
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();

        // Находим JSON внутри текста
        var start = messageContent.IndexOf('{');
        var end = messageContent.LastIndexOf('}');
        if (start >= 0 && end >= 0)
            messageContent = messageContent.Substring(start, end - start + 1);

        using var resultDoc = JsonDocument.Parse(messageContent);
        var label = resultDoc.RootElement.GetProperty("label").GetString() ?? "Unknown";
        var confidence = resultDoc.RootElement.GetProperty("confidence").GetDouble();

        return (label, Math.Round(confidence * 100, 1));
    }
    public async Task<(string Label, double Confidence)> SearchByTitleAsync(string title)
{
    var url = "https://openrouter.ai/api/v1/chat/completions";

    var payload = new
    {
        model = "nex-agi/nex-n2-pro:free",
        messages = new[]
        {
            new
            {
                role = "system",
                content = "You are a movie expert. Given a movie title, determine its genre and respond with ONLY a JSON object in this exact format: {\"label\": \"Genre\", \"confidence\": 0.95}. Choose one genre from: Science Fiction, Action, Drama, Comedy, Horror, Romance, Thriller, Animation, Documentary. Nothing else, just the JSON."
            },
            new
            {
                role = "user",
                content = $"Movie title: {title}"
            }
        }
    };

    var json = JsonSerializer.Serialize(payload);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _httpClient.PostAsync(url, content);
    var responseBody = await response.Content.ReadAsStringAsync();

    Console.WriteLine("Search response: " + responseBody);

    using var doc = JsonDocument.Parse(responseBody);
    var root = doc.RootElement;

    if (root.TryGetProperty("error", out var error))
        throw new Exception($"OpenRouter error: {error.GetProperty("message").GetString()}");

    var messageContent = root
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString() ?? "{}";

    messageContent = messageContent.Replace("```json", "").Replace("```", "").Trim();

    var start = messageContent.IndexOf('{');
    var end = messageContent.LastIndexOf('}');
    if (start >= 0 && end >= 0)
        messageContent = messageContent.Substring(start, end - start + 1);

    using var resultDoc = JsonDocument.Parse(messageContent);
    var label = resultDoc.RootElement.GetProperty("label").GetString() ?? "Unknown";
    var confidence = resultDoc.RootElement.GetProperty("confidence").GetDouble();

    return (label, Math.Round(confidence * 100, 1));
}
public async Task<List<object>> GetTopMoviesAsync(string genre)
{
    var url = "https://openrouter.ai/api/v1/chat/completions";

    var payload = new
    {
        model = "nex-agi/nex-n2-pro:free",
        messages = new[]
        {
            new
            {
                role = "system",
                content = "You are a movie expert. Given a genre, respond with ONLY a JSON array of top 5 movies in this exact format: [{\"title\": \"Movie Title\", \"year\": 2010, \"description\": \"Brief description in Russian\"}]. Nothing else, just the JSON array."
            },
            new
            {
                role = "user",
                content = $"Top 5 {genre} movies"
            }
        }
    };

    var json = JsonSerializer.Serialize(payload);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _httpClient.PostAsync(url, content);
    var responseBody = await response.Content.ReadAsStringAsync();

    Console.WriteLine("Recommend response: " + responseBody);

    using var doc = JsonDocument.Parse(responseBody);
    var root = doc.RootElement;

    if (root.TryGetProperty("error", out var error))
        throw new Exception($"OpenRouter error: {error.GetProperty("message").GetString()}");

    var messageContent = root
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString() ?? "[]";

    messageContent = messageContent.Replace("```json", "").Replace("```", "").Trim();

    var start = messageContent.IndexOf('[');
    var end = messageContent.LastIndexOf(']');
    if (start >= 0 && end >= 0)
        messageContent = messageContent.Substring(start, end - start + 1);

    using var resultDoc = JsonDocument.Parse(messageContent);
    var movies = new List<object>();

    foreach (var item in resultDoc.RootElement.EnumerateArray())
    {
        movies.Add(new
        {
            title = item.GetProperty("title").GetString(),
            year = item.GetProperty("year").GetInt32(),
            description = item.GetProperty("description").GetString()
        });
    }

    return movies;
}
}