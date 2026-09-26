using Microsoft.EntityFrameworkCore;
using PersonalKanban.Models;

namespace PersonalKanban.Data
{
    public class KanbanDbContext : DbContext
    {
        public KanbanDbContext(DbContextOptions<KanbanDbContext> options) : base(options)
        {
        }

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

            // Global Query Filter for soft delete
            modelBuilder.Entity<TaskItem>()
                .HasQueryFilter(t => !t.IsDeleted);

            // Seed Initial Data: Statuses
            modelBuilder.Entity<Status>().HasData(
                new Status { Id = 1, Name = "BACKLOG", Order = 1, Active = true },
                new Status { Id = 2, Name = "TODO", Order = 2, Active = true },
                new Status { Id = 3, Name = "IN_PROGRESS", Order = 3, Active = true },
                new Status { Id = 4, Name = "BLOCKED", Order = 4, Active = true },
                new Status { Id = 5, Name = "REVIEW", Order = 5, Active = true },
                new Status { Id = 6, Name = "DONE", Order = 6, Active = true }
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
