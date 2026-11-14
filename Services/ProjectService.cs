using Microsoft.EntityFrameworkCore;
using PersonalKanban.Data;
using PersonalKanban.Models;

namespace PersonalKanban.Services
{
    public class ProjectService
    {
        private readonly KanbanDbContext _context;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(KanbanDbContext context, ILogger<ProjectService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            return await _context.Projects
                .Include(p => p.Tasks)
                .OrderByDescending(p => p.Active)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<List<Project>> GetActiveProjectsAsync()
        {
            return await _context.Projects
                .Where(p => p.Active)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Project?> GetProjectByIdAsync(int id)
        {
            return await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Project> CreateProjectAsync(Project project)
        {
            project.CreatedAt = DateTime.Now;
            project.Active = true;
            project.OwnerId = _context.CurrentUserId
                ?? throw new InvalidOperationException("No hay usuario autenticado.");
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Project {ProjectName} created.", project.Name);
            return project;
        }

        public async Task<bool> UpdateProjectAsync(Project project)
        {
            var existing = await _context.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
            if (existing == null) return false;

            existing.Name = project.Name;
            existing.Description = project.Description;
            existing.Active = project.Active;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Project #{ProjectId} updated.", project.Id);
            return true;
        }

        public async Task<bool> ToggleProjectActiveAsync(int id)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return false;

            project.Active = !project.Active;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> DeleteProjectAsync(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return (false, "El proyecto no existe.");

            if (project.Tasks.Any())
            {
                // Deactivate instead of physical delete to preserve task relations
                project.Active = false;
                await _context.SaveChangesAsync();
                return (true, "El proyecto tiene tareas asociadas. Ha sido desactivado en lugar de eliminarse físicamente.");
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return (true, "Proyecto eliminado correctamente.");
        }
    }
}
