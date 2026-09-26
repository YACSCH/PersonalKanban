using Microsoft.EntityFrameworkCore;
using PersonalKanban.Data;
using PersonalKanban.ViewModels;

namespace PersonalKanban.Services
{
    public class DashboardService : IDashboardService
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

            var statuses = await _context.Statuses.Where(s => s.Active).ToListAsync();
            var priorities = await _context.Priorities.ToListAsync();
            var projects = await _context.Projects.ToListAsync();

            var model = new DashboardViewModel
            {
                TotalTasks = tasks.Count,
                BacklogCount = tasks.Count(t => t.Status?.Name.ToUpper() == "BACKLOG"),
                TodoCount = tasks.Count(t => t.Status?.Name.ToUpper() == "TODO"),
                InProgressCount = tasks.Count(t => t.Status?.Name.ToUpper() == "IN_PROGRESS"),
                BlockedCount = tasks.Count(t => t.Status?.Name.ToUpper() == "BLOCKED"),
                ReviewCount = tasks.Count(t => t.Status?.Name.ToUpper() == "REVIEW"),
                DoneCount = tasks.Count(t => t.Status?.Name.ToUpper() == "DONE"),
                OverdueCount = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status?.Name.ToUpper() != "DONE")
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
                .Where(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status?.Name.ToUpper() != "DONE")
                .OrderBy(t => t.DueDate)
                .Take(10)
                .ToList();

            model.RecentTasks = tasks
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .ToList();

            return model;
        }
    }
}
