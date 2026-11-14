using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalKanban.Services;
using PersonalKanban.ViewModels;

namespace PersonalKanban.Controllers
{
    public class HomeController : Controller
    {
        private readonly TaskService _taskService;
        private readonly ProjectService _projectService;
        private readonly TagService _tagService;
        private readonly SettingsService _settingsService;
        private readonly DashboardService _dashboardService;

        public HomeController(
            TaskService taskService,
            ProjectService projectService,
            TagService tagService,
            SettingsService settingsService,
            DashboardService dashboardService)
        {
            _taskService = taskService;
            _projectService = projectService;
            _tagService = tagService;
            _settingsService = settingsService;
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(TaskFilterViewModel filter)
        {
            var statuses = await _settingsService.GetStatusesAsync(onlyActive: true);
            var tasks = await _taskService.GetFilteredTasksAsync(filter);
            var projects = await _projectService.GetActiveProjectsAsync();
            var tags = await _tagService.GetAllTagsAsync();
            var priorities = await _settingsService.GetPrioritiesAsync();

            var viewModel = new KanbanBoardViewModel
            {
                Statuses = statuses,
                Tasks = tasks,
                Projects = projects,
                Priorities = priorities,
                Tags = tags,
                Filter = filter
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Dashboard()
        {
            var data = await _dashboardService.GetDashboardDataAsync();
            return View(data);
        }

        public async Task<IActionResult> Overdue()
        {
            var tasks = await _taskService.GetOverdueTasksAsync();
            return View(tasks);
        }

        [AllowAnonymous]
        public IActionResult Error() => View();
    }
}
