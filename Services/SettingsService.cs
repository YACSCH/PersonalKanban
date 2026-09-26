using Microsoft.EntityFrameworkCore;
using PersonalKanban.Data;
using PersonalKanban.Models;
using PersonalKanban.ViewModels;

namespace PersonalKanban.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly KanbanDbContext _context;

        public SettingsService(KanbanDbContext context)
        {
            _context = context;
        }

        public async Task<SettingsViewModel> GetSettingsDataAsync()
        {
            return new SettingsViewModel
            {
                Statuses = await _context.Statuses.OrderBy(s => s.Order).ToListAsync(),
                Priorities = await _context.Priorities.OrderBy(p => p.Order).ToListAsync(),
                Tags = await _context.Tags.OrderBy(t => t.Name).ToListAsync(),
                Projects = await _context.Projects.OrderBy(p => p.Name).ToListAsync()
            };
        }

        public async Task<Status> CreateStatusAsync(string name, int order)
        {
            var status = new Status
            {
                Name = name.Trim(),
                Order = order,
                Active = true
            };
            _context.Statuses.Add(status);
            await _context.SaveChangesAsync();
            return status;
        }

        public async Task<bool> UpdateStatusAsync(int id, string name, int order, bool active)
        {
            var status = await _context.Statuses.FindAsync(id);
            if (status == null) return false;

            status.Name = name.Trim();
            status.Order = order;
            status.Active = active;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Priority> CreatePriorityAsync(string name, int order)
        {
            var priority = new Priority
            {
                Name = name.Trim(),
                Order = order
            };
            _context.Priorities.Add(priority);
            await _context.SaveChangesAsync();
            return priority;
        }

        public async Task<bool> UpdatePriorityAsync(int id, string name, int order)
        {
            var priority = await _context.Priorities.FindAsync(id);
            if (priority == null) return false;

            priority.Name = name.Trim();
            priority.Order = order;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
