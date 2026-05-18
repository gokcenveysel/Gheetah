namespace Gheetah.Models.ViewModels.Dashboard;

public class MyPrsWidgetVm
{
    public string Id { get; set; }
    public List<UserPrItem> Prs { get; set; } = new();
}