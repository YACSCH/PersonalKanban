using Microsoft.AspNetCore.Mvc;
using PersonalKanban.Services;

namespace PersonalKanban.Controllers
{
    public class TagsController : Controller
    {
        private readonly ITagService _tagService;

        public TagsController(ITagService tagService)
        {
            _tagService = tagService;
        }

        public async Task<IActionResult> Index()
        {
            var tags = await _tagService.GetAllTagsAsync();
            return View(tags);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                await _tagService.CreateTagAsync(name);
                TempData["SuccessMessage"] = "Etiqueta creada correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                await _tagService.UpdateTagAsync(id, name);
                TempData["SuccessMessage"] = "Etiqueta actualizada correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _tagService.DeleteTagAsync(id);
            TempData["SuccessMessage"] = "Etiqueta eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
