# 📋 Personal Kanban — ASP.NET Core MVC & PostgreSQL

Aplicación web personal de gestión de tareas tipo **Kanban** en **ASP.NET Core MVC**, **C#**, **Entity Framework Core** y **PostgreSQL** (Docker). Cada usuario ve únicamente sus propios registros; el rol **Admin** ve todo en su panel.

---

## 🎯 Características

* **Login con correo y contraseña** (ASP.NET Core Identity). No hay registro público: solo el Admin crea cuentas.
* **Aislamiento por usuario:** tareas, proyectos y etiquetas tienen dueño; los queries globales lo garantizan incluso si se adivina un ID.
* **Rol Admin:** panel global con métricas, tareas por usuario y actividad reciente, además de alta de usuarios.
* **Tablero Kanban dinámico:** columnas desde la base (`BACKLOG`, `TODO`, `IN_PROGRESS`, `BLOCKED`, `REVIEW`, `DONE`).
* **Drag & Drop** entre columnas vía AJAX (`POST /Tasks/ChangeStatus`) con historial de cambios.
* **CRUD de tareas:** título, descripción, prioridad, proyecto, vencimiento, horas estimadas/trabajadas, etiquetas y subtareas.
* **Checklist / subtareas** con progreso porcentual.
* **Comentarios e historial** de cada modificación.
* **Filtros y búsqueda** por ID, título, descripción, proyecto, prioridad, etiqueta y vencidas.
* **Dashboard personal** y vista de tareas vencidas.
* **Proyectos y etiquetas** por usuario (los proyectos con tareas se desactivan en vez de borrarse).
* **Bootstrap 5** por CDN.

---

## 🏗️ Arquitectura

```text
Browser (HTML5, JS Drag & Drop, Bootstrap 5)
   ↓
ASP.NET Core MVC + Identity (Controllers & Razor Views)
   ↓
Services Layer (TaskService, ProjectService, TagService, DashboardService, SettingsService)
   ↓
Entity Framework Core Code First
   ↓
PostgreSQL 17 (contenedor Docker)
```

---

## 🗄️ Modelo de Datos

* **AppUser:** extiende `IdentityUser`, con `CreatedAt`.
* **Status:** `Id`, `Name`, `Order`, `Active`, `IsInitial`, `IsTerminal`.
* **Priority:** `Id`, `Name`, `Order`.
* **Project:** `Id`, `Name`, `Description`, `CreatedAt`, `Active`, `OwnerId`.
* **Tag:** `Id`, `Name`, `OwnerId`.
* **TaskItem:** `Id`, `Title`, `Description`, `StatusId`, `PriorityId`, `ProjectId`, `OwnerId`, `CreatedAt`, `StartedAt`, `DueDate`, `CompletedAt`, `EstimatedHours`, `WorkedHours`, `UpdatedAt`, `IsDeleted`.
* **TaskTag:** `TaskId`, `TagId` (muchos a muchos).
* **TaskComment:** `Id`, `TaskId`, `Comment`, `CreatedAt`.
* **TaskHistory:** `Id`, `TaskId`, `Action`, `OldValue`, `NewValue`, `CreatedAt`.
* **TaskChecklistItem:** `Id`, `TaskId`, `Description`, `Completed`, `Order`.

Los estados y prioridades son globales; tareas, proyectos y etiquetas pertenecen a un usuario.

---

## 📁 Estructura

```text
Kanban/
├── Controllers/
│   ├── AccountController.cs     # Login, logout, acceso denegado
│   ├── AdminController.cs       # Dashboard global y gestión de usuarios
│   ├── HomeController.cs        # Tablero, dashboard personal, vencidas
│   ├── TasksController.cs       # CRUD tareas, ChangeStatus, comentarios, checklist
│   ├── ProjectsController.cs    # CRUD proyectos
│   └── TagsController.cs        # CRUD etiquetas
├── Data/KanbanDbContext.cs      # Identity, filtros por usuario, seed
├── Migrations/                  # Migración InitialPostgres
├── Models/                      # Entidades
├── Services/                    # Lógica de negocio
├── ViewModels/
├── Views/                       # Home, Admin, Account, Projects, Tags, Shared
├── Tests/                       # xUnit: pruebas de aislamiento
├── scripts/import-legacy.sql    # Importación de datos SQLite anteriores
├── legacy/kanban.db             # Base SQLite original (no versionada)
├── wwwroot/                     # CSS y JS (drag & drop, modales, AJAX)
├── docker-compose.yml
├── appsettings.json
└── Program.cs
```

---

## 📦 Dependencias

* `Npgsql.EntityFrameworkCore.PostgreSQL`
* `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
* `Microsoft.EntityFrameworkCore.Design` / `Tools`
* `dotnet-ef` como tool local (`.config/dotnet-tools.json`)

---

## 🚀 Puesta en Marcha

### 1. Requisitos

* [.NET SDK 10](https://dotnet.microsoft.com/download)
* Docker Desktop

### 2. Base de datos

```bash
cp .env.example .env
# Edita .env con dos contraseñas fuertes:
#   POSTGRES_PASSWORD    -> rol bootstrap (solo administración del contenedor)
#   KANBAN_APP_PASSWORD  -> rol kanban_app que usa la aplicación
docker compose up -d          # PostgreSQL escucha solo en 127.0.0.1:5433
```

En un volumen nuevo, `scripts/db-app-role.sh` crea el rol `kanban_app` **sin superusuario** y le cede la base. Si ya tenías un volumen anterior, recrea el volumen o crea el rol y transfiere la propiedad antes de arrancar la app. `.env` está ignorado por git.

### 3. Aplicación

```bash
dotnet restore
dotnet tool restore           # instala dotnet-ef local

dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5433;Database=kanban;Username=kanban_app;Password=<KANBAN_APP_PASSWORD>"
dotnet user-secrets set "Admin:Email" "admin@kanban.local"
dotnet user-secrets set "Admin:Password" "<contraseña fuerte>"

dotnet ef database update     # crea el esquema
dotnet run
```

Al arrancar, la app aplica migraciones, crea los roles `Admin`/`User` y siembra el administrador con `Admin:Email` / `Admin:Password`. Esos valores son obligatorios en cualquier entorno: en Development salen de user-secrets; en producción, de `Admin__Email` y `Admin__Password`. La contraseña configurada solo se aplica al crear la cuenta: después, cámbiala en **Contraseña** (barra de navegación) o restablécala desde `/Admin/Users`.

Abre `http://localhost:5072` (o la URL de la consola).

---

## 👥 Usuarios y Roles

* **Admin:** ve todas las tareas, proyectos y etiquetas; crea cuentas en `/Admin/Users`.
* **User:** solo ve y modifica sus propios registros. Acceder a datos ajenos por ID devuelve 404.

---

## 🔐 Seguridad

* Contraseñas con ASP.NET Core Identity (mínimo 8 caracteres).
* Cambio de contraseña para cada usuario y restablecimiento temporal desde `/Admin/Users`.
* Bloqueo temporal tras 5 intentos fallidos y límite de 10 intentos de login por minuto e IP.
* Mensaje de login genérico: no revela si el correo existe ni si la cuenta está bloqueada.
* Antiforgery token en formularios y cabecera `RequestVerificationToken` para AJAX; cookie antiforgery `Secure` fuera de Development.
* Política de autorización por defecto: todo requiere sesión salvo login.
* CSP, `X-Content-Type-Options`, `Referrer-Policy` y `X-Frame-Options` en todas las respuestas; Bootstrap por CDN con SRI.
* PostgreSQL solo en loopback, sin contraseñas por defecto y con un rol de aplicación sin superusuario.
* Configuración de producción vía variables de entorno (`ConnectionStrings__DefaultConnection`, `Admin__Email`, `Admin__Password`, `AllowedHosts`). Fuera de Development las cookies exigen HTTPS y la app redirige HTTP a HTTPS.

---

## 💾 Copia de Seguridad

El esquema completo, sin datos, está versionado en `scripts/schema.sql`.

El backup de SQLite se retiró. Con PostgreSQL:

```bash
docker compose exec -T db pg_dump -U kanban_app -d kanban > kanban_backup.sql
docker compose exec -T db psql -U kanban_app -d kanban < kanban_backup.sql   # restaurar
```

---

## 🧪 Pruebas

```bash
dotnet test Tests/PersonalKanban.Tests.csproj
```

Verifican que un usuario solo ve sus tareas, proyectos, etiquetas, comentarios y subtareas; que no puede editar tareas ajenas; que el Admin ve todas y que el soft delete oculta registros.

---

## 📥 Importar datos de la base anterior (SQLite)

Con la app arrancada al menos una vez (para que exista el admin):

```bash
docker compose exec -T db psql -U kanban_app -d kanban \
  -v admin_email='admin@kanban.local' -f - < scripts/import-legacy.sql
```

El script es idempotente y asigna los registros al usuario indicado.
