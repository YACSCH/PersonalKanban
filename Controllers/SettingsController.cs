using Microsoft.AspNetCore.Mvc;
using PersonalKanban.Services;
using PersonalKanban.ViewModels;

namespace PersonalKanban.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;
        private readonly IBackupService _backupService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            ISettingsService settingsService,
            IBackupService backupService,
            ILogger<SettingsController> logger)
        {
            _settingsService = settingsService;
            _backupService = backupService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _settingsService.GetSettingsDataAsync();
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStatus(string name, int order)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                await _settingsService.CreateStatusAsync(name, order);
                TempData["SuccessMessage"] = "Nuevo estado guardado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStatus(int id, string name, int order, bool active)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                await _settingsService.UpdateStatusAsync(id, name, order, active);
                TempData["SuccessMessage"] = "Estado actualizado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePriority(string name, int order)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                await _settingsService.CreatePriorityAsync(name, order);
                TempData["SuccessMessage"] = "Nueva prioridad guardada correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPriority(int id, string name, int order)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                await _settingsService.UpdatePriorityAsync(id, name, order);
                TempData["SuccessMessage"] = "Prioridad actualizada correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Backup()
        {
            try
            {
                var bytes = await _backupService.GetDatabaseBackupAsync();
                var fileName = $"kanban_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                return File(bytes, "application/x-sqlite3", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar copia de seguridad.");
                TempData["ErrorMessage"] = $"No se pudo generar la copia de seguridad: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(IFormFile dbFile)
        {
            var (success, message) = await _backupService.RestoreDatabaseAsync(dbFile);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
