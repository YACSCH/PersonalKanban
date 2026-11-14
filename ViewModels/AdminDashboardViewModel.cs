namespace PersonalKanban.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }

        public Dictionary<string, int> TasksByStatus { get; set; } = new Dictionary<string, int>();
        public List<UserStats> Users { get; set; } = new List<UserStats>();
        public List<ActivityRow> RecentActivity { get; set; } = new List<ActivityRow>();

        public class UserStats
        {
            public string Email { get; set; } = string.Empty;
            public bool IsAdmin { get; set; }
            public int Total { get; set; }
            public int Completed { get; set; }
            public int Overdue { get; set; }
        }

        public class ActivityRow
        {
            public string Email { get; set; } = string.Empty;
            public string TaskTitle { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string? OldValue { get; set; }
            public string? NewValue { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
