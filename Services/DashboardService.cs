using Microsoft.EntityFrameworkCore;
using PersonalKanban.Data;
using PersonalKanban.ViewModels;

namespace PersonalKanban.Services
{
    public class DashboardService
    {
        private readonly KanbanDbContext _context;

        public DashboardService(KanbanDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var now = DateTime.Now;

            var tasks = await _context.TaskItems
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Project)
                .Include(t => t.TaskTags).ThenInclude(tt => tt.Tag)
                .Include(t => t.ChecklistItems)
                .ToListAsync();

            var statuses = await _context.Statuses.Where(s => s.Active).OrderBy(s => s.Order).ToListAsync();
            var priorities = await _context.Priorities.OrderBy(p => p.Order).ToListAsync();
            var projects = await _context.Projects.OrderBy(p => p.Name).ToListAsync();

            var model = new DashboardViewModel
            {
                TotalTasks = tasks.Count,
                CompletedTasks = tasks.Count(t => t.Status.IsTerminal),
                OverdueCount = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < now && !t.Status.IsTerminal)
            };

            foreach (var status in statuses)
            {
                model.TasksByStatus[status.Name] = tasks.Count(t => t.StatusId == status.Id);
            }

            foreach (var priority in priorities)
            {
                model.TasksByPriority[priority.Name] = tasks.Count(t => t.PriorityId == priority.Id);
            }

            foreach (var project in projects)
            {
                model.TasksByProject[project.Name] = tasks.Count(t => t.ProjectId == project.Id);
            }

            int unassignedCount = tasks.Count(t => t.ProjectId == null);
            if (unassignedCount > 0)
            {
                model.TasksByProject["Sin Proyecto"] = unassignedCount;
            }

            model.OverdueTasks = tasks
                .Where(t => t.DueDate.HasValue && t.DueDate.Value < now && !t.Status.IsTerminal)
                .OrderBy(t => t.DueDate)
                .Take(10)
                .ToList();

            model.RecentTasks = tasks
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToList();

            return model;
        }

        public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
        {
            var now = DateTime.Now;

            // El Admin ve todo: se ignoran los filtros por usuario a propósito.
            var tasks = await _context.TaskItems
                .IgnoreQueryFilters()
                .Where(t => !t.IsDeleted)
                .Include(t => t.Status)
                .Include(t => t.Owner)
                .ToListAsync();

            var users = await _context.Users.OrderBy(u => u.Email).ToListAsync();
            var adminIds = await _context.UserRoles
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Where(x => x.Name == "Admin")
                .Select(x => x.UserId)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalUsers = users.Count,
                TotalTasks = tasks.Count,
                CompletedTasks = tasks.Count(t => t.Status.IsTerminal),
                OverdueTasks = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < now && !t.Status.IsTerminal),
                TasksByStatus = tasks
                    .GroupBy(t => t.Status.Name)
                    .OrderBy(g => g.First().Status.Order)
                    .ToDictionary(g => g.Key, g => g.Count()),
                Users = users.Select(u => new AdminDashboardViewModel.UserStats
                {
                    Email = u.Email ?? u.UserName ?? "(sin correo)",
                    IsAdmin = adminIds.Contains(u.Id),
                    Total = tasks.Count(t => t.OwnerId == u.Id),
                    Completed = tasks.Count(t => t.OwnerId == u.Id && t.Status.IsTerminal),
                    Overdue = tasks.Count(t => t.OwnerId == u.Id && t.DueDate.HasValue && t.DueDate.Value < now && !t.Status.IsTerminal)
                }).ToList()
            };

            var activity = await _context.TaskHistories
                .IgnoreQueryFilters()
                .Where(h => !h.TaskItem.IsDeleted)
                .Include(h => h.TaskItem).ThenInclude(t => t.Owner)
                .OrderByDescending(h => h.CreatedAt)
                .Take(20)
                .ToListAsync();

            model.RecentActivity = activity.Select(h => new AdminDashboardViewModel.ActivityRow
            {
                Email = h.TaskItem.Owner.Email ?? h.TaskItem.OwnerId,
                TaskTitle = h.TaskItem.Title,
                Action = h.Action,
                OldValue = h.OldValue,
                NewValue = h.NewValue,
                CreatedAt = h.CreatedAt
            }).ToList();

            return model;
        }
    }
}
