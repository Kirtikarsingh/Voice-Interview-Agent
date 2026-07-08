using Backend.Interfaces;
using Backend.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Backend.Services
{
    public class GroqService : IGroqService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private const string GroqEndpoint = "https://api.groq.com/openai/v1/chat/completions";

        public GroqService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _apiKey = configuration["Groq:ApiKey"]
                ?? throw new Exception("Groq API key not found in appsettings.json");
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<AnswerEvaluationResult> EvaluateAnswerAsync(
            string question,
            string idealAnswer,
            List<string> keywords,
            string candidateAnswer,
            string language = "en")
        {
            var keywordList = string.Join(", ", keywords);
            var languageInstruction = GetLanguageInstruction(language);

            var systemPrompt = @"You are an AI interview evaluator. You will be given an interview question,
an ideal reference answer, a list of key concepts, and the candidate's actual spoken answer.

Judge whether the candidate's answer demonstrates sufficient understanding, using the keywords
as a helpful guide (the candidate does not need to say them verbatim, just show the underlying concept).

Respond ONLY with raw JSON in this exact format, no markdown, no extra text:
{
  ""isAcceptable"": true or false,
  ""feedback"": ""one or two sentence feedback for the candidate"",
  ""needsFollowUp"": true or false
}

Set needsFollowUp to true if the answer is acceptable but shallow, or if it's incomplete and probing
deeper would help assess true understanding. Set it to false if the answer is clearly strong or clearly wrong.

" + languageInstruction;

            var userPrompt = $@"Question: {question}
Ideal Answer: {idealAnswer}
Key Concepts: {keywordList}
Candidate's Answer: {candidateAnswer}";

            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.3
            };

            var jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, GroqEndpoint)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new AnswerEvaluationResult
                {
                    IsAcceptable = false,
                    Feedback = $"Groq API error: {response.StatusCode} - {responseBody}",
                    NeedsFollowUp = false
                };
            }

            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var messageContent = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                var cleaned = messageContent?
                    .Trim()
                    .Trim('`')
                    .Replace("json", "", StringComparison.OrdinalIgnoreCase)
                    .Trim() ?? "";

                var result = JsonSerializer.Deserialize<AnswerEvaluationResult>(cleaned, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new AnswerEvaluationResult
                {
                    IsAcceptable = false,
                    Feedback = "Could not evaluate the answer, please try again.",
                    NeedsFollowUp = false
                };
            }
            catch (Exception ex)
            {
                return new AnswerEvaluationResult
                {
                    IsAcceptable = false,
                    Feedback = $"Could not parse Groq response: {ex.Message}",
                    NeedsFollowUp = false
                };
            }
        }

        public async Task<InterviewFeedback> GenerateFinalFeedbackAsync(
            string role,
            List<InterviewExchange> transcript,
            string language = "en")
        {
            var transcriptText = string.Join("\n\n", transcript.Select((ex, i) =>
                $"Q{i + 1}: {ex.Question}\nCandidate Answer: {ex.Answer}\nIdeal Answer: {ex.IdealAnswer}\nEvaluator Feedback: {ex.Feedback}\nAcceptable: {ex.IsAcceptable}"));
            var languageInstruction = GetLanguageInstruction(language);

            var systemPrompt = @"You are a senior technical interviewer writing a final candidate report.
You will be given the full transcript of an interview: every question asked, the candidate's answer,
the ideal reference answer for that question, and per-answer feedback that was already given during the interview.

Based on the ENTIRE transcript, write a final structured evaluation. For EACH question in the transcript,
score the candidate's answer on three dimensions, each from 0-100:
- technicalAccuracy: how correct and technically sound the answer was compared to the ideal answer
- completeness: how much of the key concept/idea was covered, versus leaving things out
- clarity: how clearly and coherently the answer was communicated based on the transcript text

Also write a short 1-2 sentence comment per question explaining the scores given.

Respond ONLY with raw JSON in this exact format, no markdown, no extra text:
{
  ""overallScore"": 0-100 integer,
  ""summary"": ""2-3 sentence overall summary of performance"",
  ""strengths"": [""short bullet"", ""short bullet""],
  ""areasToImprove"": [""short bullet"", ""short bullet""],
  ""questionBreakdown"": [
    {
      ""question"": ""the question text"",
      ""candidateAnswer"": ""the candidate's actual answer"",
      ""idealAnswer"": ""the ideal reference answer"",
      ""technicalAccuracy"": 0-100 integer,
      ""completeness"": 0-100 integer,
      ""clarity"": 0-100 integer,
      ""comment"": ""short explanation of the scores for this question""
    }
  ]
}

" + languageInstruction;

            var userPrompt = $@"Role: {role}

Full Interview Transcript:
{transcriptText}";

            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.3
            };

            var jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, GroqEndpoint)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new InterviewFeedback
                {
                    OverallScore = 0,
                    Summary = $"Groq API error: {response.StatusCode} - {responseBody}"
                };
            }

            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var messageContent = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                var cleaned = messageContent?
                    .Trim()
                    .Trim('`')
                    .Replace("json", "", StringComparison.OrdinalIgnoreCase)
                    .Trim() ?? "";

                var result = JsonSerializer.Deserialize<InterviewFeedback>(cleaned, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new InterviewFeedback
                {
                    OverallScore = 0,
                    Summary = "Could not generate feedback."
                };
            }
            catch (Exception ex)
            {
                return new InterviewFeedback
                {
                    OverallScore = 0,
                    Summary = $"Could not parse feedback: {ex.Message}"
                };
            }
        }

        public async Task<string> TranslateTextAsync(string text, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(targetLanguage) || targetLanguage.ToLower() == "en")
            {
                return text;
            }

            var languageName = targetLanguage.ToLower() switch
            {
                "hi" => "Hindi",
                "de" => "German",
                _ => "English"
            };

            var systemPrompt = $@"You are a professional translator. Translate the given interview question into {languageName}.
Respond with ONLY the translated text. No quotes, no explanation, no extra commentary.";

            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = text }
                },
                temperature = 0.2
            };

            var jsonBody = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, GroqEndpoint)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return text;
            }

            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var translated = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return translated?.Trim() ?? text;
            }
            catch
            {
                return text;
            }
        }

        private static string GetLanguageInstruction(string language)
        {
            return language.ToLower() switch
            {
                "hi" => "IMPORTANT: Write ALL text fields in Hindi (Devanagari script) EXCEPT 'candidateAnswer' (keep that exactly as given, since it is the candidate's actual spoken words). This includes 'feedback', 'summary', 'strengths', 'areasToImprove', 'question', 'idealAnswer', and 'comment'. Keep JSON keys and boolean/numeric values in English as specified in the format.",
                "de" => "IMPORTANT: Write ALL text fields in German EXCEPT 'candidateAnswer' (keep that exactly as given, since it is the candidate's actual spoken words). This includes 'feedback', 'summary', 'strengths', 'areasToImprove', 'question', 'idealAnswer', and 'comment'. Keep JSON keys and boolean/numeric values in English as specified in the format.",
                _ => "Write all text fields in English."
            };
        }
    }
}