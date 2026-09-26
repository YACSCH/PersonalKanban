using System.ComponentModel.DataAnnotations;

namespace PersonalKanban.Models
{
    public class Status
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del estado es obligatorio.")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public int Order { get; set; }

        public bool Active { get; set; } = true;

        // Navigation property
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
