using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalKanban.Models
{
    public class TaskItem
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El título de la tarea es obligatorio.")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        [Required]
        public int StatusId { get; set; }
        public Status Status { get; set; } = null!;

        [Required]
        public int PriorityId { get; set; }
        public Priority Priority { get; set; } = null!;

        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        [ValidateNever]
        public AppUser Owner { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? StartedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }

        [Range(0, 1000)]
        public double? EstimatedHours { get; set; }

        [Range(0, 1000)]
        public double? WorkedHours { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Navigation Collections
        public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
        public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
        public ICollection<TaskHistory> History { get; set; } = new List<TaskHistory>();
        public ICollection<TaskChecklistItem> ChecklistItems { get; set; } = new List<TaskChecklistItem>();

        // Calculated Helper Properties
        [NotMapped]
        public int CompletedChecklistCount => ChecklistItems?.Count(c => c.Completed) ?? 0;

        [NotMapped]
        public int TotalChecklistCount => ChecklistItems?.Count ?? 0;

        [NotMapped]
        public int ChecklistPercentage => TotalChecklistCount == 0 ? 0 : (int)Math.Round((double)CompletedChecklistCount / TotalChecklistCount * 100);

        [NotMapped]
        public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.Now && Status?.IsTerminal != true;
    }
}
