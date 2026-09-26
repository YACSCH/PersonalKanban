using Microsoft.EntityFrameworkCore;
using PersonalKanban.Data;
using PersonalKanban.Models;

namespace PersonalKanban.Services
{
    public class TagService : ITagService
    {
        private readonly KanbanDbContext _context;

        public TagService(KanbanDbContext context)
        {
            _context = context;
        }

        public async Task<List<Tag>> GetAllTagsAsync()
        {
            return await _context.Tags
                .Include(t => t.TaskTags)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<Tag?> GetTagByIdAsync(int id)
        {
            return await _context.Tags.FindAsync(id);
        }

        public async Task<Tag> CreateTagAsync(string name)
        {
            var nameTrimmed = name.Trim();
            var existing = await _context.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == nameTrimmed.ToLower());
            if (existing != null) return existing;

            var tag = new Tag { Name = nameTrimmed };
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            return tag;
        }

        public async Task<bool> UpdateTagAsync(int id, string name)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag == null) return false;

            tag.Name = name.Trim();
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTagAsync(int id)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag == null) return false;

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
