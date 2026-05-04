namespace Gheetah.Models.EditorModel;

public class SaveFileRequest
{
    public string FilePath { get; set; }
    public string Content { get; set; }
    public string ClientHash { get; set; }
}