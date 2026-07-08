using Backend.Interfaces;
using Backend.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Backend.Services
{
    public class WhisperService : IWhisperService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private const string TranscriptionEndpoint = "https://api.groq.com/openai/v1/audio/transcriptions";

        public WhisperService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _apiKey = configuration["Groq:ApiKey"]
                ?? throw new Exception("Groq API key not found.");
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string fileName, string? language = null)
        {
            using var content = new MultipartFormDataContent();

            var audioContent = new StreamContent(audioStream);
            audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(audioContent, "file", fileName);

            content.Add(new StringContent("whisper-large-v3-turbo"), "model");
            content.Add(new StringContent("json"), "response_format");

            if (!string.IsNullOrWhiteSpace(language))
            {
                content.Add(new StringContent(language), "language");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, TranscriptionEndpoint)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new TranscriptionResult
                {
                    Success = false,
                    Error = $"Groq Whisper API error: {response.StatusCode} - {responseBody}"
                };
            }

            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var text = doc.RootElement.GetProperty("text").GetString() ?? "";

                return new TranscriptionResult
                {
                    Success = true,
                    Text = text
                };
            }
            catch (Exception ex)
            {
                return new TranscriptionResult
                {
                    Success = false,
                    Error = $"Could not parse transcription response: {ex.Message}"
                };
            }
        }
    }
}