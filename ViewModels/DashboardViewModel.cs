using PersonalKanban.Models;

namespace PersonalKanban.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalTasks { get; set; }
        public int BacklogCount { get; set; }
        public int TodoCount { get; set; }
        public int InProgressCount { get; set; }
        public int BlockedCount { get; set; }
        public int ReviewCount { get; set; }
        public int DoneCount { get; set; }
        public int OverdueCount { get; set; }

        public Dictionary<string, int> TasksByStatus { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TasksByPriority { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TasksByProject { get; set; } = new Dictionary<string, int>();

        public List<TaskItem> OverdueTasks { get; set; } = new List<TaskItem>();
        public List<TaskItem> RecentTasks { get; set; } = new List<TaskItem>();
    }
}
