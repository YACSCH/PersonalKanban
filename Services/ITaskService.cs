using PersonalKanban.Models;
using PersonalKanban.ViewModels;

namespace PersonalKanban.Services
{
    public interface ITaskService
    {
        Task<List<TaskItem>> GetFilteredTasksAsync(TaskFilterViewModel filter);
        Task<TaskItem?> GetTaskByIdAsync(int id);
        Task<TaskItem> CreateTaskAsync(TaskFormViewModel model);
        Task<bool> UpdateTaskAsync(TaskFormViewModel model);
        Task<bool> ChangeStatusAsync(int taskId, int newStatusId);
        Task<bool> SoftDeleteTaskAsync(int id);
        Task<bool> AddCommentAsync(int taskId, string commentText);
        Task<bool> AddChecklistItemAsync(int taskId, string description);
        Task<bool> ToggleChecklistItemAsync(int checklistItemId);
        Task<bool> DeleteChecklistItemAsync(int checklistItemId);
        Task<List<TaskItem>> GetOverdueTasksAsync();
    }
}
