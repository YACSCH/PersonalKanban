using System.Security.Claims;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonalKanban.Models;

namespace PersonalKanban.Data
{
    public class KanbanDbContext : IdentityDbContext<AppUser>
    {
        private readonly IHttpContextAccessor _http;

        public KanbanDbContext(DbContextOptions<KanbanDbContext> options, IHttpContextAccessor http)
            : base(options)
        {
            _http = http;
        }

        // Cada usuario solo ve sus registros; el rol Admin ve todo.
        public string? CurrentUserId => _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsAdmin => _http.HttpContext?.User.IsInRole("Admin") ?? false;

        public DbSet<TaskItem> TaskItems { get; set; } = null!;
        public DbSet<Status> Statuses { get; set; } = null!;
        public DbSet<Priority> Priorities { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<TaskTag> TaskTags { get; set; } = null!;
        public DbSet<TaskComment> TaskComments { get; set; } = null!;
        public DbSet<TaskHistory> TaskHistories { get; set; } = null!;
        public DbSet<TaskChecklistItem> TaskChecklistItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite key for TaskTag
            modelBuilder.Entity<TaskTag>()
                .HasKey(tt => new { tt.TaskId, tt.TagId });

            modelBuilder.Entity<TaskTag>()
                .HasOne(tt => tt.TaskItem)
                .WithMany(t => t.TaskTags)
                .HasForeignKey(tt => tt.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskTag>()
                .HasOne(tt => tt.Tag)
                .WithMany(t => t.TaskTags)
                .HasForeignKey(tt => tt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // TaskItem relationships
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Status)
                .WithMany(s => s.Tasks)
                .HasForeignKey(t => t.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Priority)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.PriorityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Owner)
                .WithMany()
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Owner)
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tag>()
                .HasOne(t => t.Owner)
                .WithMany()
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskItem>()
                .HasMany(t => t.Comments)
                .WithOne(c => c.TaskItem)
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskItem>()
                .HasMany(t => t.History)
                .WithOne(h => h.TaskItem)
                .HasForeignKey(h => h.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskItem>()
                .HasMany(t => t.ChecklistItems)
                .WithOne(c => c.TaskItem)
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Aislamiento por usuario (el Admin ve todo) + soft delete
            modelBuilder.Entity<TaskItem>()
                .HasQueryFilter(t => !t.IsDeleted && (IsAdmin || t.OwnerId == CurrentUserId));

            modelBuilder.Entity<Project>()
                .HasQueryFilter(p => IsAdmin || p.OwnerId == CurrentUserId);

            modelBuilder.Entity<Tag>()
                .HasQueryFilter(t => IsAdmin || t.OwnerId == CurrentUserId);

            modelBuilder.Entity<TaskTag>()
                .HasQueryFilter(tt => !tt.TaskItem.IsDeleted && (IsAdmin || tt.TaskItem.OwnerId == CurrentUserId));

            modelBuilder.Entity<TaskComment>()
                .HasQueryFilter(tc => !tc.TaskItem.IsDeleted && (IsAdmin || tc.TaskItem.OwnerId == CurrentUserId));

            modelBuilder.Entity<TaskHistory>()
                .HasQueryFilter(th => !th.TaskItem.IsDeleted && (IsAdmin || th.TaskItem.OwnerId == CurrentUserId));

            modelBuilder.Entity<TaskChecklistItem>()
                .HasQueryFilter(tci => !tci.TaskItem.IsDeleted && (IsAdmin || tci.TaskItem.OwnerId == CurrentUserId));

            // Fechas como hora local (wall clock), consistente con DateTime.Now.
            // shortcut: si se necesita multi-zona horaria, migrar a DateTime.UtcNow + timestamptz.
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
            {
                property.SetColumnType("timestamp without time zone");
            }

            // Seed Initial Data: Statuses
            modelBuilder.Entity<Status>().HasData(
                new Status { Id = 1, Name = "BACKLOG", Order = 1, Active = true, IsInitial = true },
                new Status { Id = 2, Name = "TODO", Order = 2, Active = true },
                new Status { Id = 3, Name = "IN_PROGRESS", Order = 3, Active = true },
                new Status { Id = 4, Name = "BLOCKED", Order = 4, Active = true },
                new Status { Id = 5, Name = "REVIEW", Order = 5, Active = true },
                new Status { Id = 6, Name = "DONE", Order = 6, Active = true, IsTerminal = true }
            );

            // Seed Initial Data: Priorities
            modelBuilder.Entity<Priority>().HasData(
                new Priority { Id = 1, Name = "LOW", Order = 1 },
                new Priority { Id = 2, Name = "MEDIUM", Order = 2 },
                new Priority { Id = 3, Name = "HIGH", Order = 3 },
                new Priority { Id = 4, Name = "CRITICAL", Order = 4 }
            );
        }
    }
}
