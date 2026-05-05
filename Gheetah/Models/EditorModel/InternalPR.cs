namespace Gheetah.Models.EditorModel;

public class InternalPR
{
    public string ProjectId { get; set; }
    public string PR_Id { get; set; }
    public string SourceBranch { get; set; }
    public string TargetBranch { get; set; }
    public string Status { get; set; }
    public string CreatedBy { get; set; }
    public string CreatedByEmail { get; set; }
    public List<string> Reviewers { get; set; } = new List<string>();
    public string CreatedAt { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string LastCommitHash { get; set; }
    public string Comment { get; set; }

}