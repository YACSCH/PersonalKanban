using Microsoft.AspNetCore.Mvc;
using PersonalKanban.Services;

namespace PersonalKanban.Controllers
{
    public class TagsController : Controller
    {
        private readonly TagService _tagService;

        public TagsController(TagService tagService)
        {
            _tagService = tagService;
        }

        public async Task<IActionResult> Index()
        {
            var tags = await _tagService.GetAllTagsAsync(withUsage: true);
            return View(tags);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var tag = await _tagService.CreateTagAsync(name);
                TempData[tag is null ? "ErrorMessage" : "SuccessMessage"] = tag is null
                    ? "La etiqueta debe tener entre 1 y 50 caracteres."
                    : "Etiqueta creada correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (await _tagService.UpdateTagAsync(id, name))
                    TempData["SuccessMessage"] = "Etiqueta actualizada correctamente.";
                else
                    TempData["ErrorMessage"] = "La etiqueta no existe o el nombre no es válido.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _tagService.DeleteTagAsync(id))
                TempData["SuccessMessage"] = "Etiqueta eliminada correctamente.";
            else
                TempData["ErrorMessage"] = "La etiqueta no existe.";
            return RedirectToAction(nameof(Index));
        }
    }
}
