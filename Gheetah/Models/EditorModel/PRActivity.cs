namespace Gheetah.Models.EditorModel;

public class PRActivity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PR_Id { get; set; }
    public string ActionType { get; set; }
    public string Actor { get; set; }
    public string Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}