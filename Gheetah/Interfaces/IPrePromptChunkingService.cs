using Gheetah.Models.AiModels;

namespace Gheetah.Interfaces
{
    public interface IPrePromptChunkingService
    {
        Task<List<PrePromptChunk>> ChunkAsync(string content, string prePromptId, int maxTokensPerChunk = 500);
        Task<string> RebuildAsync(List<PrePromptChunk> chunks);
        int EstimateTokenCount(string content);
        Task<List<PrePromptChunk>> SelectRelevantChunksAsync(List<PrePromptChunk> allChunks, string query, int tokenBudget);
    }
}
