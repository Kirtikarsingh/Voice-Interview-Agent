using Backend.Models;

namespace Backend.Interfaces
{
    public interface IWhisperService
    {
        Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string fileName, string? language = null);
    }
}