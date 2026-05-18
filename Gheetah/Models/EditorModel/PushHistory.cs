namespace Gheetah.Models.EditorModel;

public class PushHistory
{
    public string ProjectId { get; set; }
    public string BranchName { get; set; }
    public string OriginBranch { get; set; }
    public string CommitHash { get; set; }
    public string PushedBy { get; set; }
    public string PushedAt { get; set; }
    public bool HasPR { get; set; }
}