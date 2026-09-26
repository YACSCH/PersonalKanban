using PersonalKanban.Models;

namespace PersonalKanban.ViewModels
{
    public class SettingsViewModel
    {
        public List<Status> Statuses { get; set; } = new List<Status>();
        public List<Priority> Priorities { get; set; } = new List<Priority>();
        public List<Tag> Tags { get; set; } = new List<Tag>();
        public List<Project> Projects { get; set; } = new List<Project>();
    }
}
