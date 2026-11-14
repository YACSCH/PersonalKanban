using Microsoft.AspNetCore.Mvc;
using PersonalKanban.Models;
using PersonalKanban.Services;

namespace PersonalKanban.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly ProjectService _projectService;

        public ProjectsController(ProjectService projectService)
        {
            _projectService = projectService;
        }

        public async Task<IActionResult> Index()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            return View(projects);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return PartialView("_ProjectFormModal", new Project());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project)
        {
            if (ModelState.IsValid)
            {
                await _projectService.CreateProjectAsync(project);
                TempData["SuccessMessage"] = "Proyecto creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return PartialView("_ProjectFormModal", project);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null) return NotFound();
            return PartialView("_ProjectFormModal", project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Project project)
        {
            if (ModelState.IsValid)
            {
                if (await _projectService.UpdateProjectAsync(project))
                    TempData["SuccessMessage"] = "Proyecto actualizado correctamente.";
                else
                    TempData["ErrorMessage"] = "El proyecto no existe.";
                return RedirectToAction(nameof(Index));
            }
            return PartialView("_ProjectFormModal", project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            if (await _projectService.ToggleProjectActiveAsync(id))
                TempData["SuccessMessage"] = "Estado del proyecto actualizado.";
            else
                TempData["ErrorMessage"] = "El proyecto no existe.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _projectService.DeleteProjectAsync(id);
            if (success)
                TempData["SuccessMessage"] = message;
            else
                TempData["ErrorMessage"] = message;

            return RedirectToAction(nameof(Index));
        }
    }
}
