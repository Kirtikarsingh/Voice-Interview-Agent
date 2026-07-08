using Backend.Interfaces;
using Backend.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewController : ControllerBase
    {
        private readonly IInterviewService _interviewService;
        private readonly IRetrievalService _retrievalService;
        private readonly IGroqService _groqService;
        private readonly IWhisperService _whisperService;
        private readonly IElevenLabsService _elevenLabsService;

        public InterviewController(
            IInterviewService interviewService,
            IRetrievalService retrievalService,
            IGroqService groqService,
            IWhisperService whisperService,
            IElevenLabsService elevenLabsService)
        {
            _interviewService = interviewService;
            _retrievalService = retrievalService;
            _groqService = groqService;
            _whisperService = whisperService;
            _elevenLabsService = elevenLabsService;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartInterview([FromBody] StartInterviewRequest request)
        {
            try
            {
                var result = await _interviewService.StartInterview(request.Role, request.Language);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("answer")]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerRequest request)
        {
            var result = await _interviewService.SubmitAnswerAsync(request.SessionId, request.Answer);

            if (result.Error != null)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        [HttpGet("questions/{role}")]
        public IActionResult GetQuestions(string role)
        {
            var questions = _retrievalService.GetQuestionsByRole(role);

            if (questions.Count == 0)
            {
                return NotFound(new { message = $"No dataset found for role: {role}" });
            }

            return Ok(questions);
        }

        [HttpPost("transcribe")]
        public async Task<IActionResult> Transcribe(IFormFile audio, string? language = null)
        {
            if (audio == null || audio.Length == 0)
            {
                return BadRequest(new { message = "No audio file provided." });
            }

            using var stream = audio.OpenReadStream();
            var result = await _whisperService.TranscribeAsync(stream, audio.FileName, language);

            if (!result.Success)
            {
                return StatusCode(500, new { message = result.Error });
            }

            return Ok(new { text = result.Text });
        }

        [HttpPost("speak")]
        public async Task<IActionResult> Speak([FromBody] SpeakRequest request)
        {
            var result = await _elevenLabsService.GenerateSpeechAsync(request.Text);

            if (!result.Success || result.AudioBytes == null)
            {
                return StatusCode(500, new { message = result.Error });
            }

            return File(result.AudioBytes, "audio/mpeg", "speech.mp3");
        }
        [HttpPost("voice-answer")]
        public async Task<IActionResult> SubmitVoiceAnswer(IFormFile audio, string sessionId, string? language = null)
        {
            if (audio == null || audio.Length == 0)
            {
                return BadRequest(new { message = "No audio file provided." });
            }

            using var stream = audio.OpenReadStream();
            var transcription = await _whisperService.TranscribeAsync(stream, audio.FileName, language);

            if (!transcription.Success)
            {
                return StatusCode(500, new { message = transcription.Error });
            }

            var result = await _interviewService.SubmitAnswerAsync(sessionId, transcription.Text);

            if (result.Error != null)
            {
                return NotFound(new SubmitVoiceAnswerResponse
                {
                    SessionId = sessionId,
                    TranscribedAnswer = transcription.Text,
                    Feedback = "",
                    Error = result.Error
                });
            }

            return Ok(new SubmitVoiceAnswerResponse
            {
                SessionId = sessionId,
                TranscribedAnswer = transcription.Text,
                Feedback = result.Feedback,
                IsFollowUp = result.IsFollowUp,
                NextQuestion = result.NextQuestion,
                Completed = result.Completed,
                FinalFeedback = result.FinalFeedback
            });
        }
    }
}