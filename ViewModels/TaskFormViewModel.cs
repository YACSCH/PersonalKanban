using System.ComponentModel.DataAnnotations;
using PersonalKanban.Models;

namespace PersonalKanban.ViewModels
{
    public class TaskFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(200, ErrorMessage = "El título no puede exceder los 200 caracteres.")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio.")]
        public int StatusId { get; set; }

        [Required(ErrorMessage = "La prioridad es obligatoria.")]
        public int PriorityId { get; set; }

        public int? ProjectId { get; set; }

        public DateTime? DueDate { get; set; }

        [Range(0, 1000, ErrorMessage = "Las horas estimadas deben estar entre 0 y 1000.")]
        public double? EstimatedHours { get; set; }

        [Range(0, 1000, ErrorMessage = "Las horas trabajadas deben estar entre 0 y 1000.")]
        public double? WorkedHours { get; set; }

        public List<int> SelectedTagIds { get; set; } = new List<int>();

        public List<string> NewChecklistItems { get; set; } = new List<string>();

        // Lists for select dropdowns
        public List<Status> AvailableStatuses { get; set; } = new List<Status>();
        public List<Priority> AvailablePriorities { get; set; } = new List<Priority>();
        public List<Project> AvailableProjects { get; set; } = new List<Project>();
        public List<Tag> AvailableTags { get; set; } = new List<Tag>();
    }
}
