namespace Gheetah.Models.EditorModel;

public class AddNewFileRequest
{
    public string ParentFolderPath { get; set; }
    public string FileName { get; set; }
    public string FileType { get; set; }
    public string TemplateType { get; set; }
}