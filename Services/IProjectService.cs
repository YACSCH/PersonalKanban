using PersonalKanban.Models;

namespace PersonalKanban.Services
{
    public interface IProjectService
    {
        Task<List<Project>> GetAllProjectsAsync();
        Task<List<Project>> GetActiveProjectsAsync();
        Task<Project?> GetProjectByIdAsync(int id);
        Task<Project> CreateProjectAsync(Project project);
        Task<bool> UpdateProjectAsync(Project project);
        Task<bool> ToggleProjectActiveAsync(int id);
        Task<(bool Success, string Message)> DeleteProjectAsync(int id);
    }
}
