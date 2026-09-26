using Microsoft.AspNetCore.Mvc;
using PersonalKanban.Models;
using PersonalKanban.Services;
using PersonalKanban.ViewModels;

namespace PersonalKanban.Controllers
{
    public class TasksController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;
        private readonly ITagService _tagService;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            ITaskService taskService,
            IProjectService projectService,
            ITagService tagService,
            ISettingsService settingsService,
            ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _projectService = projectService;
            _tagService = tagService;
            _settingsService = settingsService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null)
            {
                return NotFound(new { success = false, message = "Tarea no encontrada." });
            }
            return PartialView("_TaskDetailModal", task);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var settings = await _settingsService.GetSettingsDataAsync();
            var todoStatus = settings.Statuses.FirstOrDefault(s => s.Name.ToUpper() == "TODO") ?? settings.Statuses.FirstOrDefault();

            var model = new TaskFormViewModel
            {
                StatusId = todoStatus?.Id ?? 1,
                PriorityId = settings.Priorities.FirstOrDefault(p => p.Name.ToUpper() == "MEDIUM")?.Id ?? settings.Priorities.FirstOrDefault()?.Id ?? 1,
                AvailableStatuses = settings.Statuses.Where(s => s.Active).ToList(),
                AvailablePriorities = settings.Priorities,
                AvailableProjects = await _projectService.GetActiveProjectsAsync(),
                AvailableTags = await _tagService.GetAllTagsAsync()
            };

            return PartialView("_TaskFormModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var task = await _taskService.CreateTaskAsync(model);
                return Json(new { success = true, taskId = task.Id, message = "Tarea creada correctamente." });
            }

            var settings = await _settingsService.GetSettingsDataAsync();
            model.AvailableStatuses = settings.Statuses.Where(s => s.Active).ToList();
            model.AvailablePriorities = settings.Priorities;
            model.AvailableProjects = await _projectService.GetActiveProjectsAsync();
            model.AvailableTags = await _tagService.GetAllTagsAsync();

            return PartialView("_TaskFormModal", model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null) return NotFound();

            var settings = await _settingsService.GetSettingsDataAsync();

            var model = new TaskFormViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                StatusId = task.StatusId,
                PriorityId = task.PriorityId,
                ProjectId = task.ProjectId,
                DueDate = task.DueDate,
                EstimatedHours = task.EstimatedHours,
                WorkedHours = task.WorkedHours,
                SelectedTagIds = task.TaskTags.Select(tt => tt.TagId).ToList(),
                AvailableStatuses = settings.Statuses.Where(s => s.Active).ToList(),
                AvailablePriorities = settings.Priorities,
                AvailableProjects = await _projectService.GetActiveProjectsAsync(),
                AvailableTags = await _tagService.GetAllTagsAsync()
            };

            return PartialView("_TaskFormModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TaskFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var success = await _taskService.UpdateTaskAsync(model);
                if (success)
                {
                    return Json(new { success = true, taskId = model.Id, message = "Tarea actualizada correctamente." });
                }
                return Json(new { success = false, message = "No se encontró la tarea para actualizar." });
            }

            var settings = await _settingsService.GetSettingsDataAsync();
            model.AvailableStatuses = settings.Statuses.Where(s => s.Active).ToList();
            model.AvailablePriorities = settings.Priorities;
            model.AvailableProjects = await _projectService.GetActiveProjectsAsync();
            model.AvailableTags = await _tagService.GetAllTagsAsync();

            return PartialView("_TaskFormModal", model);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ChangeStatus([FromBody] ChangeStatusRequest request)
        {
            if (request == null || request.TaskId <= 0 || request.StatusId <= 0)
            {
                return BadRequest(new { success = false, message = "Parámetros de cambio de estado inválidos." });
            }

            var result = await _taskService.ChangeStatusAsync(request.TaskId, request.StatusId);
            if (result)
            {
                return Json(new { success = true, taskId = request.TaskId, statusId = request.StatusId, message = "Estado actualizado." });
            }
            return NotFound(new { success = false, message = "No se pudo cambiar el estado de la tarea." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _taskService.SoftDeleteTaskAsync(id);
            if (result)
            {
                return Json(new { success = true, message = "Tarea eliminada correctamente." });
            }
            return NotFound(new { success = false, message = "Tarea no encontrada." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int taskId, string commentText)
        {
            var result = await _taskService.AddCommentAsync(taskId, commentText);
            if (result)
            {
                var task = await _taskService.GetTaskByIdAsync(taskId);
                return PartialView("_TaskCommentsList", task?.Comments ?? new List<TaskComment>());
            }
            return BadRequest(new { success = false, message = "El comentario no pudo ser agregado." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChecklistItem(int taskId, string description)
        {
            var result = await _taskService.AddChecklistItemAsync(taskId, description);
            if (result)
            {
                var task = await _taskService.GetTaskByIdAsync(taskId);
                return PartialView("_TaskChecklist", task);
            }
            return BadRequest(new { success = false, message = "No se pudo agregar el elemento del checklist." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleChecklistItem(int checklistItemId, int taskId)
        {
            var result = await _taskService.ToggleChecklistItemAsync(checklistItemId);
            if (result)
            {
                var task = await _taskService.GetTaskByIdAsync(taskId);
                return PartialView("_TaskChecklist", task);
            }
            return BadRequest(new { success = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChecklistItem(int checklistItemId, int taskId)
        {
            var result = await _taskService.DeleteChecklistItemAsync(checklistItemId);
            if (result)
            {
                var task = await _taskService.GetTaskByIdAsync(taskId);
                return PartialView("_TaskChecklist", task);
            }
            return BadRequest(new { success = false });
        }
    }
}
