-- Importa los datos de la base SQLite anterior (legacy/kanban.db) al usuario admin.
-- Uso, con la aplicación ya arrancada al menos una vez (crea el admin):
--   docker compose exec -T db psql -U kanban -d kanban -v admin_email='admin@kanban.local' -f - < scripts/import-legacy.sql

BEGIN;

INSERT INTO "TaskItems"
    ("Id", "Title", "Description", "StatusId", "PriorityId", "ProjectId", "OwnerId",
     "CreatedAt", "StartedAt", "DueDate", "CompletedAt", "EstimatedHours", "WorkedHours", "UpdatedAt", "IsDeleted")
SELECT 1, 'actualizacion de gateway', NULL, 2, 4, NULL, u."Id",
       '2026-09-26 19:48:48.862215', '2026-09-26 19:48:54.391532', '2026-09-28 19:48:00', NULL,
       NULL, NULL, '2026-09-26 21:39:04.898792', false
FROM "AspNetUsers" u
WHERE u."Email" = :'admin_email'
  AND NOT EXISTS (SELECT 1 FROM "TaskItems" WHERE "Id" = 1);

INSERT INTO "TaskHistories" ("Id", "TaskId", "Action", "OldValue", "NewValue", "CreatedAt")
SELECT v.id, 1, v.action, v.old_value, v.new_value, v.created_at::timestamp
FROM (VALUES
    (1, 'Creación', NULL, 'Tarea ''actualizacion de gateway'' creada', '2026-09-26 19:48:48.994193'),
    (2, 'Cambio de estado', 'BACKLOG', 'TODO', '2026-09-26 19:48:52.247939'),
    (3, 'Cambio de estado', 'TODO', 'IN_PROGRESS', '2026-09-26 19:48:54.391660'),
    (4, 'Cambio de estado', 'IN_PROGRESS', 'BACKLOG', '2026-09-26 19:49:09.551606'),
    (5, 'Cambio de prioridad', 'MEDIUM', 'CRITICAL', '2026-09-26 20:30:08.508127'),
    (6, 'Cambio de estado', 'BACKLOG', 'TO_DO', '2026-09-26 20:30:27.202252'),
    (7, 'Cambio de estado', 'TO_DO', 'IN_PROGRESS', '2026-09-26 20:30:29.006471'),
    (8, 'Cambio de estado', 'IN_PROGRESS', 'TO_DO', '2026-09-26 21:39:04.898956')
) AS v(id, action, old_value, new_value, created_at)
WHERE EXISTS (SELECT 1 FROM "TaskItems" WHERE "Id" = 1)
  AND NOT EXISTS (SELECT 1 FROM "TaskHistories" WHERE "Id" = 1);

SELECT setval(pg_get_serial_sequence('"TaskItems"', 'Id'), COALESCE((SELECT MAX("Id") FROM "TaskItems"), 1));
SELECT setval(pg_get_serial_sequence('"TaskHistories"', 'Id'), COALESCE((SELECT MAX("Id") FROM "TaskHistories"), 1));

COMMIT;
