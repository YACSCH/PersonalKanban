using Microsoft.EntityFrameworkCore;
using PersonalKanban.Data;
using PersonalKanban.Models;
using PersonalKanban.ViewModels;

namespace PersonalKanban.Services
{
    public class TaskService
    {
        private readonly KanbanDbContext _context;
        private readonly ILogger<TaskService> _logger;

        public TaskService(KanbanDbContext context, ILogger<TaskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<TaskItem>> GetFilteredTasksAsync(TaskFilterViewModel filter)
        {
            var query = _context.TaskItems
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Project)
                .Include(t => t.TaskTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(t => t.ChecklistItems)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                var isNumeric = int.TryParse(term.Replace("#", ""), out int searchId);

                query = query.Where(t =>
                    (isNumeric && t.Id == searchId) ||
                    t.Title.ToLower().Contains(term) ||
                    (t.Description != null && t.Description.ToLower().Contains(term)) ||
                    (t.Project != null && t.Project.Name.ToLower().Contains(term)) ||
                    t.TaskTags.Any(tt => tt.Tag.Name.ToLower().Contains(term))
                );
            }

            if (filter.ProjectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == filter.ProjectId.Value);
            }

            if (filter.PriorityId.HasValue)
            {
                query = query.Where(t => t.PriorityId == filter.PriorityId.Value);
            }

            if (filter.TagId.HasValue)
            {
                query = query.Where(t => t.TaskTags.Any(tt => tt.TagId == filter.TagId.Value));
            }

            if (filter.OnlyOverdue)
            {
                var now = DateTime.Now;
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value < now && !t.Status.IsTerminal);
            }

            return await query.OrderByDescending(t => t.Priority.Order).ThenByDescending(t => t.CreatedAt).ToListAsync();
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id)
        {
            return await _context.TaskItems
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Project)
                .Include(t => t.TaskTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(t => t.Comments.OrderByDescending(c => c.CreatedAt))
                .Include(t => t.History.OrderByDescending(h => h.CreatedAt))
                .Include(t => t.ChecklistItems.OrderBy(c => c.Order))
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TaskItem?> CreateTaskAsync(TaskFormViewModel model)
        {
            if (_context.CurrentUserId is null)
            {
                throw new InvalidOperationException("No hay usuario autenticado.");
            }

            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.Id == model.StatusId);
            if (status == null ||
                !await _context.Priorities.AnyAsync(p => p.Id == model.PriorityId))
            {
                return null;
            }

            if (model.ProjectId.HasValue &&
                !await _context.Projects.AnyAsync(p => p.Id == model.ProjectId.Value))
            {
                return null;
            }

            var checklistItems = model.NewChecklistItems
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => i.Trim())
                .ToList();
            if (checklistItems.Any(i => i.Length > 250))
            {
                return null;
            }

            var task = new TaskItem
            {
                Title = model.Title,
                Description = model.Description,
                StatusId = model.StatusId,
                PriorityId = model.PriorityId,
                ProjectId = model.ProjectId,
                DueDate = model.DueDate,
                EstimatedHours = model.EstimatedHours,
                WorkedHours = model.WorkedHours,
                OwnerId = _context.CurrentUserId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            if (status.Name == "IN_PROGRESS")
            {
                task.StartedAt = DateTime.Now;
            }
            if (status.IsTerminal)
            {
                task.CompletedAt = DateTime.Now;
            }

            _context.TaskItems.Add(task);

            if (model.SelectedTagIds.Count > 0)
            {
                // Solo etiquetas del usuario actual (el filtro global ya las limita).
                var validTagIds = await _context.Tags
                    .Where(t => model.SelectedTagIds.Contains(t.Id))
                    .Select(t => t.Id)
                    .ToListAsync();

                foreach (var tagId in validTagIds)
                {
                    task.TaskTags.Add(new TaskTag { TaskItem = task, TagId = tagId });
                }
            }

            int order = 1;
            foreach (var itemDesc in checklistItems)
            {
                task.ChecklistItems.Add(new TaskChecklistItem
                {
                    Description = itemDesc,
                    Completed = false,
                    Order = order++
                });
            }

            task.History.Add(new TaskHistory
            {
                Action = "Creación",
                NewValue = $"Tarea '{task.Title}' creada",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Task #{TaskId} created for {UserId}.", task.Id, task.OwnerId);
            return task;
        }

        public async Task<bool> UpdateTaskAsync(TaskFormViewModel model)
        {
            var task = await _context.TaskItems
                .Include(t => t.Status)
                .Include(t => t.TaskTags)
                .FirstOrDefaultAsync(t => t.Id == model.Id);

            if (task == null) return false;

            if (!await _context.Statuses.AnyAsync(s => s.Id == model.StatusId) ||
                !await _context.Priorities.AnyAsync(p => p.Id == model.PriorityId))
            {
                return false;
            }

            if (model.ProjectId.HasValue &&
                !await _context.Projects.AnyAsync(p => p.Id == model.ProjectId.Value))
            {
                return false;
            }

            var oldTitle = task.Title;
            var oldStatusId = task.StatusId;
            var oldPriorityId = task.PriorityId;
            var oldProjectId = task.ProjectId;
            var oldDueDate = task.DueDate;

            task.Title = model.Title;
            task.Description = model.Description;
            task.StatusId = model.StatusId;
            task.PriorityId = model.PriorityId;
            task.ProjectId = model.ProjectId;
            task.DueDate = model.DueDate;
            task.EstimatedHours = model.EstimatedHours;
            task.WorkedHours = model.WorkedHours;
            task.UpdatedAt = DateTime.Now;

            if (oldTitle != model.Title)
            {
                task.History.Add(new TaskHistory
                {
                    Action = "Modificación de tarea",
                    OldValue = oldTitle,
                    NewValue = model.Title,
                    CreatedAt = DateTime.Now
                });
            }

            if (oldPriorityId != model.PriorityId)
            {
                var oldPriority = await _context.Priorities.FindAsync(oldPriorityId);
                var newPriority = await _context.Priorities.FindAsync(model.PriorityId);
                task.History.Add(new TaskHistory
                {
                    Action = "Cambio de prioridad",
                    OldValue = oldPriority?.Name ?? oldPriorityId.ToString(),
                    NewValue = newPriority?.Name ?? model.PriorityId.ToString(),
                    CreatedAt = DateTime.Now
                });
            }

            if (oldProjectId != model.ProjectId)
            {
                var oldProj = oldProjectId.HasValue ? await _context.Projects.FindAsync(oldProjectId.Value) : null;
                var newProj = model.ProjectId.HasValue ? await _context.Projects.FindAsync(model.ProjectId.Value) : null;
                task.History.Add(new TaskHistory
                {
                    Action = "Cambio de proyecto",
                    OldValue = oldProj?.Name ?? "Ninguno",
                    NewValue = newProj?.Name ?? "Ninguno",
                    CreatedAt = DateTime.Now
                });
            }

            if (oldDueDate != model.DueDate)
            {
                task.History.Add(new TaskHistory
                {
                    Action = "Cambio de fecha",
                    OldValue = oldDueDate?.ToString("dd/MM/yyyy HH:mm") ?? "Sin fecha",
                    NewValue = model.DueDate?.ToString("dd/MM/yyyy HH:mm") ?? "Sin fecha",
                    CreatedAt = DateTime.Now
                });
            }

            if (oldStatusId != model.StatusId)
            {
                var oldStatus = await _context.Statuses.FindAsync(oldStatusId);
                var newStatus = await _context.Statuses.FindAsync(model.StatusId);

                if (newStatus != null)
                {
                    if (newStatus.Name == "IN_PROGRESS" && !task.StartedAt.HasValue)
                        task.StartedAt = DateTime.Now;

                    task.CompletedAt = newStatus.IsTerminal ? DateTime.Now : null;
                }

                task.History.Add(new TaskHistory
                {
                    Action = "Cambio de estado",
                    OldValue = oldStatus?.Name ?? oldStatusId.ToString(),
                    NewValue = newStatus?.Name ?? model.StatusId.ToString(),
                    CreatedAt = DateTime.Now
                });
            }

            _context.TaskTags.RemoveRange(task.TaskTags.Where(tt => !model.SelectedTagIds.Contains(tt.TagId)));

            if (model.SelectedTagIds.Count > 0)
            {
                var validTagIds = await _context.Tags
                    .Where(t => model.SelectedTagIds.Contains(t.Id))
                    .Select(t => t.Id)
                    .ToListAsync();

                var existingTagIds = task.TaskTags.Select(tt => tt.TagId).ToHashSet();
                foreach (var tagId in validTagIds.Where(id => !existingTagIds.Contains(id)))
                {
                    task.TaskTags.Add(new TaskTag { TaskId = task.Id, TagId = tagId });
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Task #{TaskId} updated successfully.", task.Id);
            return true;
        }

        public async Task<bool> ChangeStatusAsync(int taskId, int newStatusId)
        {
            var task = await _context.TaskItems
                .Include(t => t.Status)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return false;

            if (task.StatusId == newStatusId) return true; // No change needed

            var oldStatus = task.Status;
            var newStatus = await _context.Statuses.FindAsync(newStatusId);
            if (newStatus == null) return false;

            task.StatusId = newStatusId;
            task.UpdatedAt = DateTime.Now;

            if (newStatus.Name == "IN_PROGRESS" && !task.StartedAt.HasValue)
            {
                task.StartedAt = DateTime.Now;
            }

            task.CompletedAt = newStatus.IsTerminal ? DateTime.Now : null;

            task.History.Add(new TaskHistory
            {
                Action = "Cambio de estado",
                OldValue = oldStatus?.Name ?? "Desconocido",
                NewValue = newStatus.Name,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Task #{TaskId} status changed to {StatusName}.", task.Id, newStatus.Name);
            return true;
        }

        public async Task<bool> SoftDeleteTaskAsync(int id)
        {
            var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return false;

            task.IsDeleted = true;
            task.UpdatedAt = DateTime.Now;

            task.History.Add(new TaskHistory
            {
                Action = "Eliminación",
                OldValue = "Activo",
                NewValue = "Eliminado",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Task #{TaskId} soft deleted.", task.Id);
            return true;
        }

        public async Task<bool> AddCommentAsync(int taskId, string? commentText)
        {
            if (string.IsNullOrWhiteSpace(commentText)) return false;
            var text = commentText.Trim();
            if (text.Length == 0 || text.Length > 1000) return false;

            var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return false;

            task.Comments.Add(new TaskComment
            {
                Comment = text,
                CreatedAt = DateTime.Now
            });

            task.History.Add(new TaskHistory
            {
                Action = "Comentario agregado",
                NewValue = text.Length > 50 ? text.Substring(0, 47) + "..." : text,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddChecklistItemAsync(int taskId, string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return false;
            var text = description.Trim();
            if (text.Length == 0 || text.Length > 250) return false;

            var task = await _context.TaskItems
                .Include(t => t.ChecklistItems)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return false;

            int nextOrder = task.ChecklistItems.Count > 0 ? task.ChecklistItems.Max(c => c.Order) + 1 : 1;

            task.ChecklistItems.Add(new TaskChecklistItem
            {
                Description = text,
                Completed = false,
                Order = nextOrder
            });

            task.History.Add(new TaskHistory
            {
                Action = "Subtarea agregada",
                NewValue = text,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleChecklistItemAsync(int checklistItemId)
        {
            var item = await _context.TaskChecklistItems
                .Include(c => c.TaskItem)
                .FirstOrDefaultAsync(c => c.Id == checklistItemId);

            if (item == null) return false;

            item.Completed = !item.Completed;

            item.TaskItem.History.Add(new TaskHistory
            {
                Action = "Subtarea actualizada",
                OldValue = item.Description,
                NewValue = item.Completed ? "Completada" : "Pendiente",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteChecklistItemAsync(int checklistItemId)
        {
            var item = await _context.TaskChecklistItems
                .FirstOrDefaultAsync(c => c.Id == checklistItemId);
            if (item == null) return false;

            _context.TaskChecklistItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<TaskItem>> GetOverdueTasksAsync()
        {
            var tasks = await GetFilteredTasksAsync(new TaskFilterViewModel { OnlyOverdue = true });
            return tasks.OrderBy(t => t.DueDate).ToList();
        }
    }
}
