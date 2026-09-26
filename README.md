# 📋 Personal Kanban - ASP.NET Core MVC & SQLite

Aplicación web personal de gestión de tareas tipo **Kanban** desarrollada en **ASP.NET Core MVC**, **C#**, **Entity Framework Core** y **SQLite**.

---

## 🎯 Características Principales

* **Tablero Kanban Dinámico:** Columnas generadas dinámicamente desde SQLite (`BACKLOG`, `TODO`, `IN_PROGRESS`, `BLOCKED`, `REVIEW`, `DONE`).
* **Drag & Drop Interactivo:** Arrastra y suelta tarjetas entre columnas con actualización visual inmediata, peticiones AJAX (`POST /Tasks/ChangeStatus`) e historial registrado en tiempo real.
* **Gestión Completa de Tareas (CRUD):** Título, descripción, prioridad, proyecto, fecha de vencimiento, estimación de horas, horas trabajadas, etiquetas y subtareas.
* **Checklist / Subtareas:** Progreso porcentual visual (ej. `2/4 completadas — 50%`) con checkboxes interactivos.
* **Comentarios e Historial de Cambios:** Registro de cada cambio (creación, cambio de estado, prioridad, proyecto, fecha, modificación).
* **Filtros Avanzados y Búsqueda:** Búsqueda por ID, título, descripción, proyecto, prioridad, etiqueta y tareas vencidas.
* **Vista de Tareas Vencidas:** Identificación rápida de tareas con fecha límite superada.
* **Dashboard de Estadísticas:** Métricas generales, desglose por prioridad, desglose por proyecto y actividad reciente.
* **Gestión de Proyectos y Etiquetas:** CRUD para proyectos (desactivación lógica para proteger relaciones) y etiquetas.
* **Configuración del Sistema:** Gestión de estados y prioridades almacenados en la base de datos.
* **Backup y Restauración de SQLite:** Opción para descargar `kanban.db` y restaurar un archivo de respaldo desde la interfaz web.
* **Diseño Moderno y Responsivo:** Interfaz construida con Bootstrap 5, soporte para Modo Claro/Oscuro y tarjetas responsivas.

---

## 🏗️ Arquitectura del Sistema

```text
Browser (HTML5, JS Drag & Drop, Bootstrap 5)
   ↓
ASP.NET Core MVC (Controllers & Razor Views)
   ↓
Services Layer (TaskService, ProjectService, DashboardService, BackupService...)
   ↓
Entity Framework Core Code First
   ↓
SQLite (kanban.db)
```

---

## 🗄️ Modelo de Datos y Relaciones (ER)

* **Status:** `Id`, `Name`, `Order`, `Active`
* **Priority:** `Id`, `Name`, `Order`
* **Project:** `Id`, `Name`, `Description`, `CreatedAt`, `Active`
* **TaskItem:** `Id`, `Title`, `Description`, `StatusId`, `PriorityId`, `ProjectId`, `CreatedAt`, `StartedAt`, `DueDate`, `CompletedAt`, `EstimatedHours`, `WorkedHours`, `CreatedBy`, `UpdatedAt`, `IsDeleted`
* **Tag:** `Id`, `Name`
* **TaskTag:** `TaskId`, `TagId` *(Relación Muchos a Muchos)*
* **TaskComment:** `Id`, `TaskId`, `Comment`, `CreatedAt`
* **TaskHistory:** `Id`, `TaskId`, `Action`, `OldValue`, `NewValue`, `CreatedAt`
* **TaskChecklistItem:** `Id`, `TaskId`, `Description`, `Completed`, `Order`

---

## 📁 Estructura del Proyecto

```text
PersonalKanban/
│
├── Controllers/
│   ├── HomeController.cs       # Tablero Kanban, Dashboard y Tareas Vencidas
│   ├── TasksController.cs      # CRUD de Tareas, ChangeStatus AJAX, Comentarios, Subtareas
│   ├── ProjectsController.cs   # CRUD de Proyectos
│   ├── TagsController.cs       # CRUD de Etiquetas
│   └── SettingsController.cs   # Configuración de Estados, Prioridades y Backup/Restore
│
├── Data/
│   ├── KanbanDbContext.cs      # DbContext EF Core y Seed inicial
│   └── Migrations/             # Migraciones EF Core Code First
│
├── Models/
│   ├── TaskItem.cs
│   ├── Project.cs
│   ├── Status.cs
│   ├── Priority.cs
│   ├── Tag.cs
│   ├── TaskTag.cs
│   ├── TaskComment.cs
│   ├── TaskHistory.cs
│   └── TaskChecklistItem.cs
│
├── Services/
│   ├── TaskService.cs
│   ├── ProjectService.cs
│   ├── TagService.cs
│   ├── DashboardService.cs
│   ├── SettingsService.cs
│   └── BackupService.cs
│
├── ViewModels/
│   ├── KanbanBoardViewModel.cs
│   ├── TaskFilterViewModel.cs
│   ├── TaskFormViewModel.cs
│   ├── ChangeStatusRequest.cs
│   ├── DashboardViewModel.cs
│   └── SettingsViewModel.cs
│
├── Views/
│   ├── Home/                   # Index (Tablero), Dashboard, Overdue
│   ├── Tasks/
│   ├── Projects/
│   ├── Tags/
│   ├── Settings/
│   └── Shared/                 # _Layout, _TaskCard, Modales y Componentes parciales
│
├── wwwroot/
│   ├── css/site.css            # Estilos personalizados, variables de tema y tablero
│   └── js/kanban.js            # JavaScript de Drag & Drop, modales y llamadas AJAX
│
├── appsettings.json            # Configuración de conexión SQLite ("Data Source=kanban.db")
├── Program.cs                  # Configuración de servicios, DI y migraciones automáticas
└── PersonalKanban.csproj       # Paquetes NuGet EF Core y SQLite
```

---

## 📦 Dependencias NuGet

* `Microsoft.EntityFrameworkCore.Sqlite`
* `Microsoft.EntityFrameworkCore.Tools`
* `Microsoft.EntityFrameworkCore.Design`

---

## 🚀 Instalación y Ejecución

### 1. Requisitos Previos

* [.NET SDK 8.0 / 9.0 / 10.0](https://dotnet.microsoft.com/download)

### 2. Comandos de Inicio

Abre la terminal en la carpeta del proyecto y ejecuta:

```bash
# Restaurar paquetes NuGet
dotnet restore

# Aplicar migraciones y crear la base de datos kanban.db automáticamente
dotnet ef database update

# Ejecutar la aplicación web
dotnet run
```

Abre tu navegador web e ingresa a:
`http://localhost:5000` (o la URL indicada en la consola).

---

## 💾 Copia de Seguridad y Restauración

1. Dirígete a la sección **Configuración** (`/Settings/Index`).
2. Haz clic en **Descargar kanban.db** para guardar una copia local del archivo de base de datos.
3. Para restaurar, selecciona un archivo `.db` respaldado previamente y presiona **Restaurar** (se solicitará confirmación antes de la sustitución).
