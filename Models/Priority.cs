using System.ComponentModel.DataAnnotations;

namespace PersonalKanban.Models
{
    public class Priority
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la prioridad es obligatorio.")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public int Order { get; set; }

        // Navigation property
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
