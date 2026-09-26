using System.ComponentModel.DataAnnotations;

namespace PersonalKanban.Models
{
    public class TaskComment
    {
        [Key]
        public int Id { get; set; }

        public int TaskId { get; set; }
        public TaskItem TaskItem { get; set; } = null!;

        [Required(ErrorMessage = "El comentario no puede estar vacío.")]
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
