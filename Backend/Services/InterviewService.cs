using Backend.Interfaces;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Services
{
    public class InterviewService : IInterviewService
    {
        private readonly IRetrievalService _retrievalService;
        private readonly IGroqService _groqService;
        private readonly IInterviewSessionStore _sessionStore;

        public InterviewService(
            IRetrievalService retrievalService,
            IGroqService groqService,
            IInterviewSessionStore sessionStore)
        {
            _retrievalService = retrievalService;
            _groqService = groqService;
            _sessionStore = sessionStore;
        }

        public async Task<StartInterviewResponse> StartInterview(string role, string language = "en")
        {
            var questions = _retrievalService.GetQuestionsByRole(role);

            if (questions.Count == 0)
            {
                throw new Exception($"No questions found for role: {role}");
            }

            var ordered = questions
                .OrderBy(q => DifficultyRank(q.Difficulty))
                .ToList();

            var sessionId = Guid.NewGuid().ToString();

            var session = new InterviewSession
            {
                SessionId = sessionId,
                Role = role,
                Language = language,
                Questions = ordered
            };

            _sessionStore.CreateSession(session);

            var firstQuestion = ordered[0];
            var displayQuestion = await _groqService.TranslateTextAsync(firstQuestion.Question, language);

            return new StartInterviewResponse
            {
                SessionId = sessionId,
                Question = displayQuestion,
                Category = firstQuestion.Category,
                Difficulty = firstQuestion.Difficulty
            };
        }

        public async Task<SubmitAnswerResponse> SubmitAnswerAsync(string sessionId, string answer)
        {
            var session = _sessionStore.GetSession(sessionId);

            if (session == null)
            {
                return new SubmitAnswerResponse
                {
                    SessionId = sessionId,
                    Feedback = "",
                    Error = "Session not found. Please start a new interview."
                };
            }

            if (session.IsComplete)
            {
                return new SubmitAnswerResponse
                {
                    SessionId = sessionId,
                    Feedback = "Interview already completed.",
                    Completed = true
                };
            }

            var currentQuestion = session.Questions[session.CurrentQuestionIndex];

            string questionTextAsked = (session.AwaitingFollowUp && session.FollowUpIndexUsed >= 0)
                ? currentQuestion.FollowUps[session.FollowUpIndexUsed]
                : currentQuestion.Question;

            var evaluation = await _groqService.EvaluateAnswerAsync(
                questionTextAsked,
                currentQuestion.IdealAnswer,
                currentQuestion.Keywords,
                answer,
                session.Language);

            session.Transcript.Add(new InterviewExchange
            {
                Question = questionTextAsked,
                Answer = answer,
                IdealAnswer = currentQuestion.IdealAnswer,
                Feedback = evaluation.Feedback,
                IsAcceptable = evaluation.IsAcceptable
            });

            // Case 1: This was a main question, and it needs a follow-up
            if (!session.AwaitingFollowUp && evaluation.NeedsFollowUp && currentQuestion.FollowUps.Count > 0)
            {
                session.AwaitingFollowUp = true;
                session.FollowUpIndexUsed = 0;

                var translatedFollowUp = await _groqService.TranslateTextAsync(currentQuestion.FollowUps[0], session.Language);

                return new SubmitAnswerResponse
                {
                    SessionId = sessionId,
                    Feedback = evaluation.Feedback,
                    IsFollowUp = true,
                    NextQuestion = translatedFollowUp,
                    Completed = false
                };
            }

            // Case 2: Move on to the next main question (either follow-up just finished,
            // or no follow-up was needed)
            session.AwaitingFollowUp = false;
            session.FollowUpIndexUsed = -1;
            session.CurrentQuestionIndex++;

            if (session.CurrentQuestionIndex >= session.Questions.Count)
            {
                session.IsComplete = true;

                var finalFeedback = await _groqService.GenerateFinalFeedbackAsync(session.Role, session.Transcript, session.Language);

                return new SubmitAnswerResponse
                {
                    SessionId = sessionId,
                    Feedback = evaluation.Feedback,
                    Completed = true,
                    NextQuestion = null,
                    FinalFeedback = finalFeedback
                };
            }

            var nextQuestion = session.Questions[session.CurrentQuestionIndex];
            var translatedNext = await _groqService.TranslateTextAsync(nextQuestion.Question, session.Language);

            return new SubmitAnswerResponse
            {
                SessionId = sessionId,
                Feedback = evaluation.Feedback,
                IsFollowUp = false,
                NextQuestion = translatedNext,
                Completed = false
            };
        }

        public async Task<SubmitAnswerResponse> EndInterviewEarlyAsync(string sessionId)
        {
            var session = _sessionStore.GetSession(sessionId);

            if (session == null)
            {
                return new SubmitAnswerResponse
                {
                    SessionId = sessionId,
                    Feedback = "",
                    Error = "Session not found."
                };
            }

            if (session.Transcript.Count == 0)
            {
                return new SubmitAnswerResponse
                {
                    SessionId = sessionId,
                    Feedback = "",
                    Error = "No answers have been submitted yet."
                };
            }

            session.IsComplete = true;

            var finalFeedback = await _groqService.GenerateFinalFeedbackAsync(
                session.Role, session.Transcript, session.Language);

            return new SubmitAnswerResponse
            {
                SessionId = sessionId,
                Feedback = "Interview ended early by candidate.",
                Completed = true,
                NextQuestion = null,
                FinalFeedback = finalFeedback
            };
        }

        private static int DifficultyRank(string difficulty)
        {
            return difficulty.ToLower() switch
            {
                "easy" => 0,
                "medium" => 1,
                "hard" => 2,
                _ => 3
            };
        }
    }
}