namespace Gheetah.Models.EditorModel;

public class IntelliSenseData
{
    public string Language { get; set; }
    public List<string> Namespaces { get; set; } = new();
    public List<StepDefinitionInfo> StepDefinitions { get; set; } = new();
}

public class StepDefinitionInfo
{
    public string Type { get; set; }
    public string Pattern { get; set; }
    public string FileName { get; set; }
    public int LineNumber { get; set; }
}