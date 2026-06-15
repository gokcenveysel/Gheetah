using Gheetah.Interfaces;
using Gheetah.Models.AiModels;

namespace Gheetah.Services
{
    public class PrePromptChunkingService : IPrePromptChunkingService
    {
        private const int CharsPerToken = 4;

        public int EstimateTokenCount(string content)
            => string.IsNullOrEmpty(content) ? 0 : content.Length / CharsPerToken;

        public async Task<List<PrePromptChunk>> ChunkAsync(string content, string prePromptId, int maxTokensPerChunk = 500)
        {
            if (string.IsNullOrEmpty(content))
                return new List<PrePromptChunk>();

            var paragraphs = content
                .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            var chunks = new List<PrePromptChunk>();
            var order = 0;
            var current = new List<string>();
            var currentTokens = 0;

            foreach (var para in paragraphs)
            {
                var paraTokens = EstimateTokenCount(para);

                if (currentTokens + paraTokens > maxTokensPerChunk && current.Any())
                {
                    chunks.Add(BuildChunk(prePromptId, order++, current, content));
                    current.Clear();
                    currentTokens = 0;
                }

                current.Add(para);
                currentTokens += paraTokens;
            }

            if (current.Any())
                chunks.Add(BuildChunk(prePromptId, order, current, content));

            return await Task.FromResult(chunks);
        }

        public async Task<string> RebuildAsync(List<PrePromptChunk> chunks)
        {
            var ordered = chunks.OrderBy(c => c.Order).Select(c => c.Content);
            return await Task.FromResult(string.Join(Environment.NewLine + Environment.NewLine, ordered));
        }

        public async Task<List<PrePromptChunk>> SelectRelevantChunksAsync(
            List<PrePromptChunk> allChunks, string query, int tokenBudget)
        {
            if (!allChunks.Any()) return new List<PrePromptChunk>();

            var queryKeywords = ExtractKeywords(query);
            var scored = allChunks.Select(chunk => new
            {
                Chunk = chunk,
                Score = chunk.Keywords.Count(k => queryKeywords.Contains(k, StringComparer.OrdinalIgnoreCase))
            })
            .OrderByDescending(x => x.Score)
            .ToList();

            var selected = new List<PrePromptChunk>();
            var usedTokens = 0;

            foreach (var item in scored)
            {
                if (usedTokens + item.Chunk.TokenCount > tokenBudget) break;
                selected.Add(item.Chunk);
                usedTokens += item.Chunk.TokenCount;
            }

            return await Task.FromResult(selected.OrderBy(c => c.Order).ToList());
        }

        private static PrePromptChunk BuildChunk(string prePromptId, int order, List<string> paragraphs, string fullContent)
        {
            var chunkContent = string.Join(Environment.NewLine + Environment.NewLine, paragraphs);
            return new PrePromptChunk
            {
                PrePromptId = prePromptId,
                Order = order,
                Content = chunkContent,
                TokenCount = chunkContent.Length / CharsPerToken,
                Keywords = ExtractKeywords(chunkContent),
                ChunkType = DetermineChunkType(chunkContent)
            };
        }

        private static List<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();

            return text
                .Split(new[] { ' ', '\n', '\r', '\t', ',', '.', ';', ':', '(', ')', '[', ']', '{', '}' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .Select(w => w.ToLowerInvariant())
                .Distinct()
                .Take(50)
                .ToList();
        }

        private static PrePromptChunkType DetermineChunkType(string content)
        {
            if (content.Contains("[StepDefinition]") || content.Contains("@Given") || content.Contains("@When"))
                return PrePromptChunkType.StepDefinition;
            if (content.TrimStart().StartsWith("Scenario:") || content.TrimStart().StartsWith("Feature:"))
                return PrePromptChunkType.Scenario;
            if (content.Contains("config") || content.Contains("settings") || content.Contains("appsettings"))
                return PrePromptChunkType.Config;
            if (content.StartsWith("Example:") || content.StartsWith("For example"))
                return PrePromptChunkType.Example;
            if (content.StartsWith("Note:") || content.StartsWith("IMPORTANT") || content.StartsWith("Constraint"))
                return PrePromptChunkType.Constraint;
            return PrePromptChunkType.Context;
        }
    }
}
