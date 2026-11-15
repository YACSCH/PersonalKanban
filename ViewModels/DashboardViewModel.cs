using PersonalKanban.Models;

namespace PersonalKanban.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalTasks { get; set; }
        public int ActiveTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueCount { get; set; }
        public int CompletionRate { get; set; }

        public List<DistributionItem> TasksByStatus { get; set; } = new List<DistributionItem>();
        public List<DistributionItem> TasksByPriority { get; set; } = new List<DistributionItem>();
        public List<DistributionItem> TasksByProject { get; set; } = new List<DistributionItem>();

        public List<TaskItem> OverdueTasks { get; set; } = new List<TaskItem>();
        public List<TaskItem> RecentTasks { get; set; } = new List<TaskItem>();
    }

    public class DistributionItem
    {
        // 0 = fila sin entidad asociada (por ejemplo, "Sin Proyecto").
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
