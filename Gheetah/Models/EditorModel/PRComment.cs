namespace Gheetah.Models.EditorModel;

public enum CommentStatus { Active, Resolved }

public class PRComment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ParentId { get; set; }
    public string PR_Id { get; set; }
    public string FilePath { get; set; }
    public int LineNumber { get; set; }
    public string Author { get; set; }
    public string Content { get; set; }
    public CommentStatus Status { get; set; } = CommentStatus.Active;
    public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}