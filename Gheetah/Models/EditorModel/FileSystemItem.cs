using Newtonsoft.Json;

namespace Gheetah.Models.EditorModel;

public class FileSystemItem
{
    [JsonProperty("text")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("id")]
    public string FullPath { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = "file";

    [JsonProperty("children")]
    public List<FileSystemItem>? Children { get; set; }
    
    [JsonProperty("icon")]
    public string Icon => Type == "folder" ? "fa fa-folder text-yellow" : GetIconForExtension();

    private string GetIconForExtension()
    {
        if (Name.EndsWith(".cs")) return "fa fa-file-code text-blue";
        if (Name.EndsWith(".java")) return "fa fa-file-code text-danger";
        if (Name.EndsWith(".feature")) return "fa fa-file-text text-success";
        return "fa fa-file text-secondary";
    }
}