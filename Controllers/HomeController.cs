using Microsoft.AspNetCore.Mvc;
using PersonalKanban.Services;
using PersonalKanban.ViewModels;

namespace PersonalKanban.Controllers
{
    public class HomeController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;
        private readonly ITagService _tagService;
        private readonly ISettingsService _settingsService;
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ITaskService taskService,
            IProjectService projectService,
            ITagService tagService,
            ISettingsService settingsService,
            IDashboardService dashboardService,
            ILogger<HomeController> logger)
        {
            _taskService = taskService;
            _projectService = projectService;
            _tagService = tagService;
            _settingsService = settingsService;
            _dashboardService = dashboardService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(TaskFilterViewModel filter)
        {
            var settings = await _settingsService.GetSettingsDataAsync();
            var activeStatuses = settings.Statuses.Where(s => s.Active).OrderBy(s => s.Order).ToList();
            var tasks = await _taskService.GetFilteredTasksAsync(filter);
            var projects = await _projectService.GetActiveProjectsAsync();
            var tags = await _tagService.GetAllTagsAsync();

            var viewModel = new KanbanBoardViewModel
            {
                Statuses = activeStatuses,
                Tasks = tasks,
                Projects = projects,
                Priorities = settings.Priorities.OrderBy(p => p.Order).ToList(),
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
    }
}
