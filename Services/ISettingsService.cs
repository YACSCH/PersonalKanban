using PersonalKanban.Models;
using PersonalKanban.ViewModels;

namespace PersonalKanban.Services
{
    public interface ISettingsService
    {
        Task<SettingsViewModel> GetSettingsDataAsync();
        Task<Status> CreateStatusAsync(string name, int order);
        Task<bool> UpdateStatusAsync(int id, string name, int order, bool active);
        Task<Priority> CreatePriorityAsync(string name, int order);
        Task<bool> UpdatePriorityAsync(int id, string name, int order);
    }
}
