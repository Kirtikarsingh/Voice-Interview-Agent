using Backend.Models;

namespace Backend.Interfaces
{
    public interface IElevenLabsService
    {
        Task<SpeechResult> GenerateSpeechAsync(string text);
    }
}