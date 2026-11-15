using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalKanban.Models;
using PersonalKanban.Services;
using PersonalKanban.ViewModels;

namespace PersonalKanban.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly DashboardService _dashboardService;
        private readonly UserManager<AppUser> _userManager;

        public AdminController(DashboardService dashboardService, UserManager<AppUser> userManager)
        {
            _dashboardService = dashboardService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await _dashboardService.GetAdminDashboardAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var stats = await _dashboardService.GetAdminDashboardAsync();
            ViewBag.AdminIds = admins.Select(a => a.Id).ToHashSet();
            ViewBag.TaskCounts = stats.Users.ToDictionary(u => u.Email, u => u.Total);
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Revisa el correo y usa una contraseña de al menos 8 caracteres.";
                return RedirectToAction(nameof(Users));
            }

            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Users));
            }

            await _userManager.AddToRoleAsync(user, model.IsAdmin ? "Admin" : "User");
            TempData["SuccessMessage"] = $"Usuario {model.Email} creado correctamente.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "El usuario no existe.";
                return RedirectToAction(nameof(Users));
            }

            var temporaryPassword = TemporaryPassword.Create();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, temporaryPassword);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Users));
            }

            TempData["SuccessMessage"] =
                $"Contraseña temporal de {user.Email}: {temporaryPassword} (cámbiala al iniciar sesión).";
            return RedirectToAction(nameof(Users));
        }
    }
}
