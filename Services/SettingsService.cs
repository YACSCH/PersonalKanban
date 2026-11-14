using Microsoft.EntityFrameworkCore;
using PersonalKanban.Data;
using PersonalKanban.Models;

namespace PersonalKanban.Services
{
    public class SettingsService
    {
        private readonly KanbanDbContext _context;

        public SettingsService(KanbanDbContext context)
        {
            _context = context;
        }

        public async Task<List<Status>> GetStatusesAsync(bool onlyActive = false)
        {
            var query = _context.Statuses.AsQueryable();
            if (onlyActive) query = query.Where(s => s.Active);
            return await query.OrderBy(s => s.Order).ToListAsync();
        }

        public async Task<List<Priority>> GetPrioritiesAsync()
        {
            return await _context.Priorities.OrderBy(p => p.Order).ToListAsync();
        }
    }
}
