using Microsoft.AspNetCore.Identity;

namespace PersonalKanban.Models
{
    public class AppUser : IdentityUser
    {
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
