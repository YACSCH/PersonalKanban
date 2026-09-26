using System.ComponentModel.DataAnnotations;

namespace PersonalKanban.Models
{
    public class Tag
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la etiqueta es obligatorio.")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        // Navigation property
        public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    }
}
