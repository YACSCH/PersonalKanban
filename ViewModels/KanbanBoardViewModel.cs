using PersonalKanban.Models;

namespace PersonalKanban.ViewModels
{
    public class KanbanBoardViewModel
    {
        public List<Status> Statuses { get; set; } = new List<Status>();
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public List<Project> Projects { get; set; } = new List<Project>();
        public List<Priority> Priorities { get; set; } = new List<Priority>();
        public List<Tag> Tags { get; set; } = new List<Tag>();
        public TaskFilterViewModel Filter { get; set; } = new TaskFilterViewModel();
    }
}
