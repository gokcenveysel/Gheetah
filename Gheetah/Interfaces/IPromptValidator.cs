using Gheetah.Models.AiModels;

namespace Gheetah.Interfaces
{
    public interface IPromptValidator
    {
        Task<ValidationResult> ValidateInputAsync(string promptContent, ValidationContext context);
        Task<ValidationResult> ValidateBddOutputAsync(string gherkinContent);
    }
}
