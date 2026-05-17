namespace Gheetah.Models.ViewModels.Dashboard;

public class UserPrItem
{
    public string PR_Id { get; set; }
    public string Title { get; set; }
    public string SourceBranch { get; set; }
    public string TargetBranch { get; set; }
    public string Status { get; set; } // Open, Approved, Rejected, Merged, etc.
    public string CreatedBy { get; set; }
    public string CreatedAt { get; set; }
    public string ReviewStatus { get; set; } // pending, approved, rejected
}