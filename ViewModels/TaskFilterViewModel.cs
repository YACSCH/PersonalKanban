namespace PersonalKanban.ViewModels
{
    public class TaskFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public int? ProjectId { get; set; }
        public int? PriorityId { get; set; }
        public int? TagId { get; set; }
        public string? DateFilter { get; set; } // "overdue", "today", "this_week", etc.
        public bool OnlyOverdue { get; set; }
    }
}
