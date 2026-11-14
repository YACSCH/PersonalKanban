using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PersonalKanban.Models
{
    public class Tag
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la etiqueta es obligatorio.")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public string OwnerId { get; set; } = string.Empty;
        [ValidateNever]
        public AppUser Owner { get; set; } = null!;

        // Navigation property
        public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    }
}
