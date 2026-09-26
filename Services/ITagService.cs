using PersonalKanban.Models;

namespace PersonalKanban.Services
{
    public interface ITagService
    {
        Task<List<Tag>> GetAllTagsAsync();
        Task<Tag?> GetTagByIdAsync(int id);
        Task<Tag> CreateTagAsync(string name);
        Task<bool> UpdateTagAsync(int id, string name);
        Task<bool> DeleteTagAsync(int id);
    }
}
