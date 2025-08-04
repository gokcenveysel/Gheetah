namespace Gheetah.Models.ScenarioModel
{
    public class TestStep
    {
        public string StepDefinition { get; set; }
        public string StepName { get; set; }
        public string Status { get; set; }
        public List<string> Details { get; set; }
        public string ErrorMessage { get; set; }
        public long Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public string ScreenshotPath { get; set; }

        public TestStep()
        {
            Details = new List<string>();
            Parameters = new Dictionary<string, string>();
            StartTime = DateTime.Now;
        }

        public void CompleteStep(string status, string errorMessage = null)
        {
            Status = status;
            EndTime = DateTime.Now;
            Duration = (long)(EndTime - StartTime).TotalMilliseconds;
            ErrorMessage = errorMessage;
        }
    }
}
