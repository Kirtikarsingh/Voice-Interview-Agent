using Backend.Interfaces;
using Backend.Models;
using System.Text;
using System.Text.Json;

namespace Backend.Services
{
    public class ElevenLabsService : IElevenLabsService
    {
        private readonly string _apiKey;
        private readonly string _voiceId;
        private readonly HttpClient _httpClient;

        public ElevenLabsService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _apiKey = configuration["ElevenLabs:ApiKey"]
                ?? throw new Exception("ElevenLabs API key not found.");
            _voiceId = configuration["ElevenLabs:VoiceId"]
                ?? "21m00Tcm4TlvDq8ikWAM"; // Rachel, default fallback
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<SpeechResult> GenerateSpeechAsync(string text)
        {
            var endpoint = $"https://api.elevenlabs.io/v1/text-to-speech/{_voiceId}";

            var requestBody = new
            {
                text = text,
                model_id = "eleven_multilingual_v2",
                voice_settings = new
                {
                    stability = 0.5,
                    similarity_boost = 0.75
                }
            };

            var jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };
            request.Headers.Add("xi-api-key", _apiKey);
            request.Headers.Add("Accept", "audio/mpeg");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return new SpeechResult
                {
                    Success = false,
                    Error = $"ElevenLabs API error: {response.StatusCode} - {errorBody}"
                };
            }

            var audioBytes = await response.Content.ReadAsByteArrayAsync();

            return new SpeechResult
            {
                Success = true,
                AudioBytes = audioBytes
            };
        }
    }
}