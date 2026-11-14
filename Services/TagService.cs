using Microsoft.EntityFrameworkCore;
using PersonalKanban.Data;
using PersonalKanban.Models;

namespace PersonalKanban.Services
{
    public class TagService
    {
        private readonly KanbanDbContext _context;

        public TagService(KanbanDbContext context)
        {
            _context = context;
        }

        public async Task<List<Tag>> GetAllTagsAsync(bool withUsage = false)
        {
            var query = _context.Tags.AsQueryable();
            if (withUsage) query = query.Include(t => t.TaskTags);
            return await query.OrderBy(t => t.Name).ToListAsync();
        }

        public async Task<Tag?> CreateTagAsync(string name)
        {
            var ownerId = _context.CurrentUserId
                ?? throw new InvalidOperationException("No hay usuario autenticado.");
            var nameTrimmed = name.Trim();
            if (nameTrimmed.Length == 0 || nameTrimmed.Length > 50) return null;

            var existing = await _context.Tags
                .FirstOrDefaultAsync(t => t.OwnerId == ownerId && t.Name.ToLower() == nameTrimmed.ToLower());
            if (existing != null) return existing;

            var tag = new Tag { Name = nameTrimmed, OwnerId = ownerId };
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            return tag;
        }

        public async Task<bool> UpdateTagAsync(int id, string name)
        {
            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == id);
            if (tag == null) return false;

            var nameTrimmed = name.Trim();
            if (nameTrimmed.Length == 0 || nameTrimmed.Length > 50) return false;

            tag.Name = nameTrimmed;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTagAsync(int id)
        {
            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == id);
            if (tag == null) return false;

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
