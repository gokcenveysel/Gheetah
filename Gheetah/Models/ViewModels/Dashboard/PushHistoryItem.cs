namespace Gheetah.Models.ViewModels.Dashboard;

public class PushHistoryItem
{
    public string ProjectName { get; set; }
    public string BranchName { get; set; }
    public string CommitHash { get; set; }
    public string PushedBy { get; set; }
    public string PushedAt { get; set; }
    public bool HasPR { get; set; }
}