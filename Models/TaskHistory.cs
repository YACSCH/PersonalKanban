using System.ComponentModel.DataAnnotations;

namespace PersonalKanban.Models
{
    public class TaskHistory
    {
        [Key]
        public int Id { get; set; }

        public int TaskId { get; set; }
        public TaskItem TaskItem { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(250)]
        public string? OldValue { get; set; }

        [StringLength(250)]
        public string? NewValue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
