namespace Gheetah.Models.ProcessModel
{
    public class ProcessInfo
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public ProcessStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public List<string> Output { get; set; } = new();
        public string HtmlReport { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public string HangfireJobId { get; set; }
    }
}
