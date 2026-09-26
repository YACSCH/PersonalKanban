using Microsoft.EntityFrameworkCore;
using PersonalKanban.Data;
using PersonalKanban.Models;
using PersonalKanban.ViewModels;

namespace PersonalKanban.Services
{
    public class TaskService : ITaskService
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
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status.Name.ToUpper() != "DONE");
            }

            if (!string.IsNullOrWhiteSpace(filter.DateFilter))
            {
                var now = DateTime.Now.Date;
                if (filter.DateFilter == "today")
                {
                    query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == now);
                }
                else if (filter.DateFilter == "week")
                {
                    var endOfWeek = now.AddDays(7);
                    query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date >= now && t.DueDate.Value.Date <= endOfWeek);
                }
                else if (filter.DateFilter == "overdue")
                {
                    query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.Now && t.Status.Name.ToUpper() != "DONE");
                }
            }

            return await query.OrderBy(t => t.Priority.Order).ThenByDescending(t => t.CreatedAt).ToListAsync();
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

        public async Task<TaskItem> CreateTaskAsync(TaskFormViewModel model)
        {
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
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // Set StartedAt or CompletedAt if initially created in IN_PROGRESS or DONE
            var status = await _context.Statuses.FindAsync(model.StatusId);
            if (status != null)
            {
                if (status.Name.ToUpper() == "IN_PROGRESS")
                {
                    task.StartedAt = DateTime.Now;
                }
                else if (status.Name.ToUpper() == "DONE")
                {
                    task.CompletedAt = DateTime.Now;
                }
            }

            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();

            // Tags mapping
            if (model.SelectedTagIds != null && model.SelectedTagIds.Any())
            {
                foreach (var tagId in model.SelectedTagIds)
                {
                    _context.TaskTags.Add(new TaskTag { TaskId = task.Id, TagId = tagId });
                }
            }

            // Initial Checklist items
            if (model.NewChecklistItems != null && model.NewChecklistItems.Any())
            {
                int order = 1;
                foreach (var itemDesc in model.NewChecklistItems)
                {
                    if (!string.IsNullOrWhiteSpace(itemDesc))
                    {
                        _context.TaskChecklistItems.Add(new TaskChecklistItem
                        {
                            TaskId = task.Id,
                            Description = itemDesc.Trim(),
                            Completed = false,
                            Order = order++
                        });
                    }
                }
            }

            // History Log
            _context.TaskHistories.Add(new TaskHistory
            {
                TaskId = task.Id,
                Action = "Creación",
                NewValue = $"Tarea '{task.Title}' creada",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Task #{TaskId} created successfully.", task.Id);
            return task;
        }

        public async Task<bool> UpdateTaskAsync(TaskFormViewModel model)
        {
            var task = await _context.TaskItems
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Project)
                .Include(t => t.TaskTags)
                .FirstOrDefaultAsync(t => t.Id == model.Id);

            if (task == null) return false;

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

            // Log changes
            if (oldTitle != model.Title)
            {
                _context.TaskHistories.Add(new TaskHistory
                {
                    TaskId = task.Id,
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
                _context.TaskHistories.Add(new TaskHistory
                {
                    TaskId = task.Id,
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
                _context.TaskHistories.Add(new TaskHistory
                {
                    TaskId = task.Id,
                    Action = "Cambio de proyecto",
                    OldValue = oldProj?.Name ?? "Ninguno",
                    NewValue = newProj?.Name ?? "Ninguno",
                    CreatedAt = DateTime.Now
                });
            }

            if (oldDueDate != model.DueDate)
            {
                _context.TaskHistories.Add(new TaskHistory
                {
                    TaskId = task.Id,
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
                    if (newStatus.Name.ToUpper() == "IN_PROGRESS" && !task.StartedAt.HasValue)
                        task.StartedAt = DateTime.Now;

                    if (newStatus.Name.ToUpper() == "DONE")
                        task.CompletedAt = DateTime.Now;
                    else
                        task.CompletedAt = null;
                }

                _context.TaskHistories.Add(new TaskHistory
                {
                    TaskId = task.Id,
                    Action = "Cambio de estado",
                    OldValue = oldStatus?.Name ?? oldStatusId.ToString(),
                    NewValue = newStatus?.Name ?? model.StatusId.ToString(),
                    CreatedAt = DateTime.Now
                });
            }

            // Update Tags
            _context.TaskTags.RemoveRange(task.TaskTags);
            if (model.SelectedTagIds != null && model.SelectedTagIds.Any())
            {
                foreach (var tagId in model.SelectedTagIds)
                {
                    _context.TaskTags.Add(new TaskTag { TaskId = task.Id, TagId = tagId });
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

            if (newStatus.Name.ToUpper() == "IN_PROGRESS" && !task.StartedAt.HasValue)
            {
                task.StartedAt = DateTime.Now;
            }

            if (newStatus.Name.ToUpper() == "DONE")
            {
                task.CompletedAt = DateTime.Now;
            }
            else
            {
                task.CompletedAt = null;
            }

            _context.TaskHistories.Add(new TaskHistory
            {
                TaskId = task.Id,
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
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null) return false;

            task.IsDeleted = true;
            task.UpdatedAt = DateTime.Now;

            _context.TaskHistories.Add(new TaskHistory
            {
                TaskId = task.Id,
                Action = "Eliminación",
                OldValue = "Activo",
                NewValue = "Eliminado",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("Task #{TaskId} soft deleted.", task.Id);
            return true;
        }

        public async Task<bool> AddCommentAsync(int taskId, string commentText)
        {
            if (string.IsNullOrWhiteSpace(commentText)) return false;

            var task = await _context.TaskItems.FindAsync(taskId);
            if (task == null) return false;

            var comment = new TaskComment
            {
                TaskId = taskId,
                Comment = commentText.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.TaskComments.Add(comment);

            _context.TaskHistories.Add(new TaskHistory
            {
                TaskId = taskId,
                Action = "Comentario agregado",
                NewValue = commentText.Length > 50 ? commentText.Substring(0, 47) + "..." : commentText,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddChecklistItemAsync(int taskId, string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return false;

            var task = await _context.TaskItems
                .Include(t => t.ChecklistItems)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return false;

            int nextOrder = task.ChecklistItems.Count > 0 ? task.ChecklistItems.Max(c => c.Order) + 1 : 1;

            var item = new TaskChecklistItem
            {
                TaskId = taskId,
                Description = description.Trim(),
                Completed = false,
                Order = nextOrder
            };

            _context.TaskChecklistItems.Add(item);

            _context.TaskHistories.Add(new TaskHistory
            {
                TaskId = taskId,
                Action = "Subtarea agregada",
                NewValue = description.Trim(),
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

            _context.TaskHistories.Add(new TaskHistory
            {
                TaskId = item.TaskId,
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
            var item = await _context.TaskChecklistItems.FindAsync(checklistItemId);
            if (item == null) return false;

            _context.TaskChecklistItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<TaskItem>> GetOverdueTasksAsync()
        {
            var now = DateTime.Now;
            return await _context.TaskItems
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.Project)
                .Include(t => t.TaskTags).ThenInclude(tt => tt.Tag)
                .Include(t => t.ChecklistItems)
                .Where(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status.Name.ToUpper() != "DONE")
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }
    }
}
