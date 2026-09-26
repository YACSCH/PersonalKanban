using System.ComponentModel.DataAnnotations;

namespace PersonalKanban.Models
{
    public class TaskChecklistItem
    {
        [Key]
        public int Id { get; set; }

        public int TaskId { get; set; }
        public TaskItem TaskItem { get; set; } = null!;

        [Required(ErrorMessage = "La descripción de la subtarea es obligatoria.")]
        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        public bool Completed { get; set; } = false;

        public int Order { get; set; } = 0;
    }
}
