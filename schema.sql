-- ============================================================================
-- Base de Datos: Personal Kanban (SQLite)
-- Archivo: schema.sql
-- Descripción: Esquema DDL para tablas, relaciones, índices y datos iniciales.
-- ============================================================================

-- Habilitar soporte de llaves foráneas en SQLite
PRAGMA foreign_keys = ON;

BEGIN TRANSACTION;

-- ----------------------------------------------------------------------------
-- 1. Tabla: Priorities (Prioridades de las tareas)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Priorities" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Order" INTEGER NOT NULL
);

-- ----------------------------------------------------------------------------
-- 2. Tabla: Statuses (Estados del flujo Kanban)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Statuses" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Order" INTEGER NOT NULL,
    "Active" INTEGER NOT NULL DEFAULT 1
);

-- ----------------------------------------------------------------------------
-- 3. Tabla: Projects (Proyectos)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Projects" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "Active" INTEGER NOT NULL DEFAULT 1
);

-- ----------------------------------------------------------------------------
-- 4. Tabla: Tags (Etiquetas)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Tags" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL
);

-- ----------------------------------------------------------------------------
-- 5. Tabla: TaskItems (Tareas principales)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "TaskItems" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "Title" TEXT NOT NULL,
    "Description" TEXT NULL,
    "StatusId" INTEGER NOT NULL,
    "PriorityId" INTEGER NOT NULL,
    "ProjectId" INTEGER NULL,
    "CreatedAt" TEXT NOT NULL,
    "StartedAt" TEXT NULL,
    "DueDate" TEXT NULL,
    "CompletedAt" TEXT NULL,
    "EstimatedHours" REAL NULL,
    "WorkedHours" REAL NULL,
    "CreatedBy" TEXT NULL,
    "UpdatedAt" TEXT NULL,
    "IsDeleted" INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT "FK_TaskItems_Statuses_StatusId" FOREIGN KEY ("StatusId") REFERENCES "Statuses" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_TaskItems_Priorities_PriorityId" FOREIGN KEY ("PriorityId") REFERENCES "Priorities" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_TaskItems_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE SET NULL
);

-- ----------------------------------------------------------------------------
-- 6. Tabla: TaskChecklistItems (Checklist / Subtareas)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "TaskChecklistItems" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "TaskId" INTEGER NOT NULL,
    "Description" TEXT NOT NULL,
    "Completed" INTEGER NOT NULL DEFAULT 0,
    "Order" INTEGER NOT NULL,
    CONSTRAINT "FK_TaskChecklistItems_TaskItems_TaskId" FOREIGN KEY ("TaskId") REFERENCES "TaskItems" ("Id") ON DELETE CASCADE
);

-- ----------------------------------------------------------------------------
-- 7. Tabla: TaskComments (Comentarios de tareas)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "TaskComments" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "TaskId" INTEGER NOT NULL,
    "Comment" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_TaskComments_TaskItems_TaskId" FOREIGN KEY ("TaskId") REFERENCES "TaskItems" ("Id") ON DELETE CASCADE
);

-- ----------------------------------------------------------------------------
-- 8. Tabla: TaskHistories (Historial / Auditoría de cambios)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "TaskHistories" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "TaskId" INTEGER NOT NULL,
    "Action" TEXT NOT NULL,
    "OldValue" TEXT NULL,
    "NewValue" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_TaskHistories_TaskItems_TaskId" FOREIGN KEY ("TaskId") REFERENCES "TaskItems" ("Id") ON DELETE CASCADE
);

-- ----------------------------------------------------------------------------
-- 9. Tabla: TaskTags (Relación N:M entre Tareas y Etiquetas)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "TaskTags" (
    "TaskId" INTEGER NOT NULL,
    "TagId" INTEGER NOT NULL,
    CONSTRAINT "PK_TaskTags" PRIMARY KEY ("TaskId", "TagId"),
    CONSTRAINT "FK_TaskTags_TaskItems_TaskId" FOREIGN KEY ("TaskId") REFERENCES "TaskItems" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_TaskTags_Tags_TagId" FOREIGN KEY ("TagId") REFERENCES "Tags" ("Id") ON DELETE CASCADE
);

-- ----------------------------------------------------------------------------
-- Índices
-- ----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS "IX_TaskItems_StatusId" ON "TaskItems" ("StatusId");
CREATE INDEX IF NOT EXISTS "IX_TaskItems_PriorityId" ON "TaskItems" ("PriorityId");
CREATE INDEX IF NOT EXISTS "IX_TaskItems_ProjectId" ON "TaskItems" ("ProjectId");
CREATE INDEX IF NOT EXISTS "IX_TaskChecklistItems_TaskId" ON "TaskChecklistItems" ("TaskId");
CREATE INDEX IF NOT EXISTS "IX_TaskComments_TaskId" ON "TaskComments" ("TaskId");
CREATE INDEX IF NOT EXISTS "IX_TaskHistories_TaskId" ON "TaskHistories" ("TaskId");
CREATE INDEX IF NOT EXISTS "IX_TaskTags_TagId" ON "TaskTags" ("TagId");

-- ----------------------------------------------------------------------------
-- Datos Iniciales (Seed Data)
-- ----------------------------------------------------------------------------
INSERT OR IGNORE INTO "Priorities" ("Id", "Name", "Order") VALUES
(1, 'LOW', 1),
(2, 'MEDIUM', 2),
(3, 'HIGH', 3),
(4, 'CRITICAL', 4);

INSERT OR IGNORE INTO "Statuses" ("Id", "Name", "Order", "Active") VALUES
(1, 'BACKLOG', 1, 1),
(2, 'TODO', 2, 1),
(3, 'IN_PROGRESS', 3, 1),
(4, 'BLOCKED', 4, 1),
(5, 'REVIEW', 5, 1),
(6, 'DONE', 6, 1);

COMMIT;
