namespace Gheetah.Models.EditorModel;

public class PRSnapshot
{
    public string PR_Id { get; set; }
    public string ProjectId { get; set; }
    public string SourceBranch { get; set; }
    public string TargetBranch { get; set; }
    public string LastCommitHash { get; set; }
    public List<ChangedFileSnapshot> ChangedFiles { get; set; } = new();
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}

public class ChangedFileSnapshot
{
    public string FilePath { get; set; }
    public string OriginalContent { get; set; }
    public string ModifiedContent { get; set; }
    public string BlobHash { get; set; }
}