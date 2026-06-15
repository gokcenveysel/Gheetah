namespace Gheetah.Models.AiModels
{
    public class ValidationResult
    {
        public bool IsValid => !BlockingErrors.Any();
        public List<ValidationError> BlockingErrors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();
        public int Score { get; set; } = 100;
        public List<string> Suggestions { get; set; } = new();
    }

    public class ValidationError
    {
        public string RuleId { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; } = "error";
        public int? Line { get; set; }
    }

    public class ValidationWarning
    {
        public string RuleId { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; } = "warning";
        public bool UserCanProceed { get; set; } = true;
    }

    public class ValidationContext
    {
        public string ProjectType { get; set; }
        public string Source { get; set; }
        public int MaxTokenBudget { get; set; } = 3000;
        public bool BlockOnSemanticWarnings { get; set; }
    }
}
