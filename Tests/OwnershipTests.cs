using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PersonalKanban.Data;
using PersonalKanban.Models;
using PersonalKanban.Services;
using PersonalKanban.ViewModels;
using Xunit;

namespace PersonalKanban.Tests
{
    public class OwnershipTests
    {
        private static IHttpContextAccessor Accessor(string? userId, bool admin = false)
        {
            var claims = new List<Claim>();
            if (userId != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            if (admin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

            return new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
                }
            };
        }

        private static KanbanDbContext CreateDb(string name, IHttpContextAccessor accessor)
        {
            var options = new DbContextOptionsBuilder<KanbanDbContext>()
                .UseInMemoryDatabase(name)
                .Options;
            return new KanbanDbContext(options, accessor);
        }

        private static void Seed(string name)
        {
            using var db = CreateDb(name, Accessor(null, admin: true));
            db.TaskItems.AddRange(
                new TaskItem { Id = 1, Title = "De Ana", StatusId = 1, PriorityId = 1, OwnerId = "ana" },
                new TaskItem { Id = 2, Title = "De Beto", StatusId = 1, PriorityId = 1, OwnerId = "beto" },
                new TaskItem { Id = 3, Title = "Borrada de Ana", StatusId = 1, PriorityId = 1, OwnerId = "ana", IsDeleted = true });
            db.SaveChanges();
        }

        [Fact]
        public async Task Usuario_solo_ve_sus_tareas()
        {
            var name = Guid.NewGuid().ToString();
            Seed(name);

            using var db = CreateDb(name, Accessor("ana"));
            var titles = await db.TaskItems.Select(t => t.Title).ToListAsync();

            Assert.Equal(new[] { "De Ana" }, titles);
        }

        [Fact]
        public async Task Admin_ve_las_tareas_de_todos()
        {
            var name = Guid.NewGuid().ToString();
            Seed(name);

            using var db = CreateDb(name, Accessor("admin", admin: true));
            var titles = await db.TaskItems.Select(t => t.Title).OrderBy(t => t).ToListAsync();

            Assert.Equal(new[] { "De Ana", "De Beto" }, titles);
        }

        [Fact]
        public async Task Soft_delete_oculta_la_tarea_para_su_dueno()
        {
            var name = Guid.NewGuid().ToString();
            Seed(name);

            using var db = CreateDb(name, Accessor("ana"));
            var titles = await db.TaskItems.Select(t => t.Title).ToListAsync();

            Assert.DoesNotContain("Borrada de Ana", titles);
        }

        [Fact]
        public async Task Usuario_solo_ve_proyectos_etiquetas_comentarios_y_subtareas_propias()
        {
            var name = Guid.NewGuid().ToString();
            using (var db = CreateDb(name, Accessor(null, admin: true)))
            {
                db.TaskItems.AddRange(
                    new TaskItem { Id = 1, Title = "De Ana", StatusId = 1, PriorityId = 1, OwnerId = "ana" },
                    new TaskItem { Id = 2, Title = "De Beto", StatusId = 1, PriorityId = 1, OwnerId = "beto" });
                db.Projects.AddRange(
                    new Project { Id = 1, Name = "Proyecto de Ana", OwnerId = "ana" },
                    new Project { Id = 2, Name = "Proyecto de Beto", OwnerId = "beto" });
                db.Tags.AddRange(
                    new Tag { Id = 1, Name = "etiqueta-ana", OwnerId = "ana" },
                    new Tag { Id = 2, Name = "etiqueta-beto", OwnerId = "beto" });
                db.TaskComments.AddRange(
                    new TaskComment { Id = 1, TaskId = 1, Comment = "Comentario de Ana" },
                    new TaskComment { Id = 2, TaskId = 2, Comment = "Comentario de Beto" });
                db.TaskChecklistItems.AddRange(
                    new TaskChecklistItem { Id = 1, TaskId = 1, Description = "Subtarea de Ana" },
                    new TaskChecklistItem { Id = 2, TaskId = 2, Description = "Subtarea de Beto" });
                db.SaveChanges();
            }

            using var anaDb = CreateDb(name, Accessor("ana"));
            Assert.Equal(new[] { "Proyecto de Ana" }, await anaDb.Projects.Select(p => p.Name).ToListAsync());
            Assert.Equal(new[] { "etiqueta-ana" }, await anaDb.Tags.Select(t => t.Name).ToListAsync());
            Assert.Equal(new[] { "Comentario de Ana" }, await anaDb.TaskComments.Select(c => c.Comment).ToListAsync());
            Assert.Equal(new[] { "Subtarea de Ana" }, await anaDb.TaskChecklistItems.Select(c => c.Description).ToListAsync());
        }

        [Fact]
        public async Task No_se_puede_editar_la_tarea_de_otro_usuario()
        {
            var name = Guid.NewGuid().ToString();
            using (var db = CreateDb(name, Accessor(null, admin: true)))
            {
                db.Statuses.Add(new Status { Id = 1, Name = "BACKLOG", Order = 1 });
                db.Priorities.Add(new Priority { Id = 1, Name = "LOW", Order = 1 });
                db.TaskItems.AddRange(
                    new TaskItem { Id = 1, Title = "De Ana", StatusId = 1, PriorityId = 1, OwnerId = "ana" },
                    new TaskItem { Id = 2, Title = "De Beto", StatusId = 1, PriorityId = 1, OwnerId = "beto" });
                db.SaveChanges();
            }

            using (var anaDb = CreateDb(name, Accessor("ana")))
            {
                var service = new TaskService(anaDb, NullLogger<TaskService>.Instance);
                var updated = await service.UpdateTaskAsync(new TaskFormViewModel
                {
                    Id = 2,
                    Title = "Hackeada",
                    StatusId = 1,
                    PriorityId = 1
                });

                Assert.False(updated);
            }

            using var adminDb = CreateDb(name, Accessor(null, admin: true));
            Assert.Equal("De Beto", (await adminDb.TaskItems.SingleAsync(t => t.Id == 2)).Title);
        }
    }
}
