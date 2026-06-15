using Gheetah.Interfaces;
using Gheetah.Models.AiModels;

namespace Gheetah.Services
{
    public class PromptValidator : IPromptValidator
    {
        private readonly ILogger<PromptValidator> _logger;

        public PromptValidator(ILogger<PromptValidator> logger)
        {
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateInputAsync(string promptContent, ValidationContext context)
        {
            var result = new ValidationResult();

            // STRUCT-001: Empty input
            if (string.IsNullOrWhiteSpace(promptContent))
            {
                result.BlockingErrors.Add(new ValidationError
                {
                    RuleId = "STRUCT-001",
                    Message = "Prompt content cannot be empty."
                });
                result.Score = 0;
                return result;
            }

            // STRUCT-002: Minimum length
            if (promptContent.Trim().Length < 10)
            {
                result.BlockingErrors.Add(new ValidationError
                {
                    RuleId = "STRUCT-002",
                    Message = "Prompt is too short (minimum 10 characters). Please provide more detail."
                });
                result.Score = 0;
                return result;
            }

            // STRUCT-003: Token budget exceeded
            var estimatedTokens = promptContent.Length / 4;
            if (estimatedTokens > (context?.MaxTokenBudget ?? 3000))
            {
                result.BlockingErrors.Add(new ValidationError
                {
                    RuleId = "STRUCT-003",
                    Message = $"Prompt exceeds the token budget ({estimatedTokens} estimated > {context?.MaxTokenBudget ?? 3000} max). Please shorten it."
                });
                result.Score = 20;
                return result;
            }

            // SEM-001: No testable action (no verbs)
            var actionVerbs = new[] { "click", "enter", "verify", "check", "navigate", "open", "close",
                "select", "fill", "submit", "confirm", "search", "add", "delete", "update", "login",
                "logout", "create", "tıkla", "gir", "doğrula", "kontrol", "aç", "seç", "ara" };
            var lowerContent = promptContent.ToLowerInvariant();
            if (!actionVerbs.Any(v => lowerContent.Contains(v)))
            {
                result.Warnings.Add(new ValidationWarning
                {
                    RuleId = "SEM-001",
                    Message = "Prompt may not contain testable actions. Consider adding verbs like 'click', 'verify', 'navigate'.",
                    UserCanProceed = !(context?.BlockOnSemanticWarnings ?? false)
                });
                result.Score -= 15;
            }

            // SEM-003: Too many independent scenarios
            var scenarioIndicators = new[] { "scenario", "case", "when", "also", "additionally", "furthermore", "senaryo", "durum" };
            var scenarioCount = scenarioIndicators.Sum(s => CountOccurrences(lowerContent, s));
            if (scenarioCount > 5)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    RuleId = "SEM-003",
                    Message = $"Prompt suggests {scenarioCount} scenarios. Consider splitting into separate prompts for better quality.",
                    UserCanProceed = true
                });
                result.Score -= 10;
            }

            // Unclosed template variables
            var openBraces = CountOccurrences(promptContent, "{{");
            var closeBraces = CountOccurrences(promptContent, "}}");
            if (openBraces != closeBraces)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    RuleId = "SEM-004",
                    Message = "Unclosed template variable detected (mismatched {{ }}). Check your variable syntax.",
                    UserCanProceed = true
                });
                result.Score -= 5;
            }

            result.Score = Math.Max(0, result.Score);
            return await Task.FromResult(result);
        }

        public async Task<ValidationResult> ValidateBddOutputAsync(string gherkinContent)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(gherkinContent))
            {
                result.BlockingErrors.Add(new ValidationError
                {
                    RuleId = "BDD-001",
                    Message = "Generated BDD content is empty."
                });
                return result;
            }

            var lines = gherkinContent.Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            // Count steps
            var stepLines = lines.Where(l =>
                l.StartsWith("Given ") || l.StartsWith("When ") || l.StartsWith("Then ") || l.StartsWith("And ")).ToList();

            // BDD-001: Too few steps
            if (stepLines.Count < 3)
            {
                result.BlockingErrors.Add(new ValidationError
                {
                    RuleId = "BDD-001",
                    Message = $"Scenario has only {stepLines.Count} steps (minimum 3 required)."
                });
            }

            // BDD-002: Too many steps
            if (stepLines.Count > 50)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    RuleId = "BDD-002",
                    Message = $"Scenario has {stepLines.Count} steps which may be overly complex. Consider splitting."
                });
            }

            // STRUCT-004: Consecutive Given steps
            var consecutiveGivens = 0;
            var maxConsecutiveGivens = 0;
            foreach (var line in stepLines)
            {
                if (line.StartsWith("Given ")) { consecutiveGivens++; maxConsecutiveGivens = Math.Max(maxConsecutiveGivens, consecutiveGivens); }
                else consecutiveGivens = 0;
            }
            if (maxConsecutiveGivens >= 3)
            {
                result.BlockingErrors.Add(new ValidationError
                {
                    RuleId = "STRUCT-004",
                    Message = $"Found {maxConsecutiveGivens} consecutive 'Given' steps. BDD scenarios should not chain multiple givens."
                });
            }

            // STRUCT-005: No Then steps
            if (!stepLines.Any(l => l.StartsWith("Then ")))
            {
                result.BlockingErrors.Add(new ValidationError
                {
                    RuleId = "STRUCT-005",
                    Message = "Scenario has no 'Then' steps. Add at least one assertion."
                });
            }

            // STRUCT-006: Repeated steps
            var stepTexts = stepLines.Select(l => l.ToLowerInvariant()).ToList();
            var repeatedSteps = stepTexts.GroupBy(s => s).Where(g => g.Count() >= 3).Select(g => g.Key).ToList();
            if (repeatedSteps.Any())
            {
                result.BlockingErrors.Add(new ValidationError
                {
                    RuleId = "STRUCT-006",
                    Message = $"Step repeated 3+ times: \"{repeatedSteps.First()}\". This may indicate an infinite loop."
                });
            }

            result.Score = result.IsValid ? 100 : 0;
            return await Task.FromResult(result);
        }

        private static int CountOccurrences(string text, string pattern)
        {
            var count = 0;
            var index = 0;
            while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }
    }
}
