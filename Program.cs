using Microsoft.EntityFrameworkCore;
using PersonalKanban.Data;
using PersonalKanban.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure SQLite DbContext with rooted path
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=kanban.db";
var connectionStringBuilder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(rawConnectionString);
if (!Path.IsPathRooted(connectionStringBuilder.DataSource))
{
    connectionStringBuilder.DataSource = Path.Combine(builder.Environment.ContentRootPath, connectionStringBuilder.DataSource);
}
var connectionString = connectionStringBuilder.ToString();

builder.Services.AddDbContext<KanbanDbContext>(options =>
    options.UseSqlite(connectionString));

// Register Application Services
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IBackupService, BackupService>();

var app = builder.Build();

// Auto-apply pending EF Core migrations on application startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<KanbanDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Verificando y aplicando migraciones pendientes en SQLite...");
        dbContext.Database.Migrate();
        logger.LogInformation("Base de datos SQLite sincronizada correctamente.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al inicializar la base de datos SQLite.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
