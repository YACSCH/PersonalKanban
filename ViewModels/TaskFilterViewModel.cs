namespace PersonalKanban.ViewModels
{
    public class TaskFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public int? ProjectId { get; set; }
        public int? PriorityId { get; set; }
        public int? TagId { get; set; }
        public bool OnlyOverdue { get; set; }
    }
}
