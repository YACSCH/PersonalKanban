using PersonalKanban.ViewModels;

namespace PersonalKanban.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync();
    }
}
