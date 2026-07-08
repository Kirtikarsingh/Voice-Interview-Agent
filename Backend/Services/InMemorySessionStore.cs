using System.Collections.Concurrent;
using Backend.Interfaces;
using Backend.Models;

namespace Backend.Services
{
    public class InMemorySessionStore : IInterviewSessionStore
    {
        private readonly ConcurrentDictionary<string, InterviewSession> _sessions = new();

        public void CreateSession(InterviewSession session)
        {
            _sessions[session.SessionId] = session;
        }

        public InterviewSession? GetSession(string sessionId)
        {
            return _sessions.TryGetValue(sessionId, out var session) ? session : null;
        }
    }
}