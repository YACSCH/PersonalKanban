using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PersonalKanban.Data;
using PersonalKanban.Models;
using PersonalKanban.Services;
using Xunit;

namespace PersonalKanban.Tests
{
    public class DashboardTests
    {
        private static IHttpContextAccessor Accessor(string? userId)
        {
            var claims = new List<Claim>();
            if (userId != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

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

        [Fact]
        public async Task Calcula_resumen_y_solo_cuenta_tareas_del_dueno()
        {
            var name = Guid.NewGuid().ToString();

            using (var db = CreateDb(name, Accessor("ana")))
            {
                db.Statuses.AddRange(
                    new Status { Id = 1, Name = "BACKLOG", Order = 1 },
                    new Status { Id = 2, Name = "DONE", Order = 2, IsTerminal = true });
                db.Priorities.AddRange(
                    new Priority { Id = 1, Name = "LOW", Order = 1 },
                    new Priority { Id = 2, Name = "HIGH", Order = 2 });
                db.Projects.Add(new Project { Id = 1, Name = "Proyecto", OwnerId = "ana" });
                db.TaskItems.AddRange(
                    new TaskItem
                    {
                        Id = 1, Title = "Vencida", StatusId = 1, PriorityId = 2, ProjectId = 1,
                        OwnerId = "ana", DueDate = DateTime.Now.AddDays(-1)
                    },
                    new TaskItem
                    {
                        Id = 2, Title = "Activa", StatusId = 1, PriorityId = 1,
                        OwnerId = "ana", DueDate = DateTime.Now.AddDays(3)
                    },
                    new TaskItem
                    {
                        Id = 3, Title = "Hecha", StatusId = 2, PriorityId = 1,
                        OwnerId = "ana", CompletedAt = DateTime.Now
                    });
                db.SaveChanges();
            }

            using var anaDb = CreateDb(name, Accessor("ana"));
            var data = await new DashboardService(anaDb).GetDashboardDataAsync();

            Assert.Equal(3, data.TotalTasks);
            Assert.Equal(2, data.ActiveTasks);
            Assert.Equal(1, data.CompletedTasks);
            Assert.Equal(1, data.OverdueCount);
            Assert.Equal(33, data.CompletionRate);
            Assert.Equal(2, data.TasksByStatus.Single(s => s.Name == "BACKLOG").Count);
            Assert.Equal(1, data.TasksByStatus.Single(s => s.Name == "DONE").Count);
            Assert.Equal(1, data.TasksByProject.Single(p => p.Name == "Proyecto").Count);
            Assert.Equal(2, data.TasksByProject.Single(p => p.Name == "Sin Proyecto").Count);
            Assert.Equal("Vencida", data.OverdueTasks.Single().Title);
        }
    }
}
