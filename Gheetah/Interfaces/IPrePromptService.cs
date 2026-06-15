using Gheetah.Models.AiModels;

namespace Gheetah.Interfaces
{
    public interface IPrePromptService
    {
        Task<string> BuildPrePromptAsync(string prePromptId, Dictionary<string, string> context);
        Task SavePrePromptAsync(string prePromptId, string rawContent);
        Task<string> GetRawContentAsync(string prePromptId);
        Task<ValidationResult> ValidatePrePromptAsync(string content);
        string GenerateFromRequirements(List<string> testTypes, string targetUrl, Dictionary<string, string> requirements);
    }
}
