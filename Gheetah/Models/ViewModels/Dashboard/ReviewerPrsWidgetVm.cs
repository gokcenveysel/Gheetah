namespace Gheetah.Models.ViewModels.Dashboard;

public class ReviewerPrsWidgetVm
{
    public string Id { get; set; } // Widget ID
    public List<UserPrItem> Prs { get; set; } = new();
}