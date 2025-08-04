using System.ComponentModel.DataAnnotations;

namespace Gheetah.Models.ViewModels.Dashboard
{
    public class DashboardWidgetVm
    {
        public string Id { get; set; }
        [Required]
        public string Type { get; set; }
        [Required]
        public string Title { get; set; }
        public int ColumnSize { get; set; } = 6;
    
        public string CicdToolId { get; set; }
        public int CicdToolType { get; set; }
        public string Project { get; set; }
        public int PipelineId { get; set; }
        public string PipelineName { get; set; }
    
        public string ChartType { get; set; } = "bar";
    }
}
