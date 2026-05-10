namespace Gheetah.Models.EditorModel;

public class MergeBuildProgress
{
    public string PrId { get; set; }
    public string SourceBuildStatus { get; set; } = "pending";
    public string MergeStatus { get; set; } = "pending";
    public string TargetBuildStatus { get; set; } = "pending";
    public string SourceBuildMessage { get; set; }
    public string MergeMessage { get; set; }
    public string TargetBuildMessage { get; set; }
    public DateTime? SourceBuildStartTime { get; set; }
    public DateTime? SourceBuildEndTime { get; set; }
    public DateTime? MergeStartTime { get; set; }
    public DateTime? MergeEndTime { get; set; }
    public DateTime? TargetBuildStartTime { get; set; }
    public DateTime? TargetBuildEndTime { get; set; }
    public bool IsCompleted => SourceBuildStatus == "success" && MergeStatus == "success" && TargetBuildStatus == "success";
    public bool HasError => SourceBuildStatus.StartsWith("fail") || MergeStatus.StartsWith("fail") || TargetBuildStatus.StartsWith("fail");
}