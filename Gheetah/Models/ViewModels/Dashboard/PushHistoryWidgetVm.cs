namespace Gheetah.Models.ViewModels.Dashboard;

public class PushHistoryWidgetVm
{
    public string Id { get; set; }
    public List<PushHistoryItem> Histories { get; set; } = new();
}