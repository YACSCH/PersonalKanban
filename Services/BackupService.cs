using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalKanban.Data;

namespace PersonalKanban.Services
{
    public class BackupService : IBackupService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<BackupService> _logger;

        public BackupService(IWebHostEnvironment env, IConfiguration config, ILogger<BackupService> logger)
        {
            _env = env;
            _config = config;
            _logger = logger;
        }

        private string GetDbPath()
        {
            var connString = _config.GetConnectionString("DefaultConnection") ?? "Data Source=kanban.db";
            var builder = new SqliteConnectionStringBuilder(connString);
            var dataSource = builder.DataSource;

            if (Path.IsPathRooted(dataSource))
            {
                return dataSource;
            }
            return Path.Combine(_env.ContentRootPath, dataSource);
        }

        public async Task<byte[]> GetDatabaseBackupAsync()
        {
            var dbPath = GetDbPath();
            _logger.LogInformation("Creating backup from {DbPath}", dbPath);

            SqliteConnection.ClearAllPools();

            if (!File.Exists(dbPath))
            {
                throw new FileNotFoundException("El archivo kanban.db no existe aún.");
            }

            // Create a temporary copy to read safely without file locking issues
            var tempBackupPath = Path.Combine(Path.GetTempPath(), $"kanban_backup_{Guid.NewGuid()}.db");
            File.Copy(dbPath, tempBackupPath, overwrite: true);

            var bytes = await File.ReadAllBytesAsync(tempBackupPath);

            if (File.Exists(tempBackupPath))
            {
                File.Delete(tempBackupPath);
            }

            return bytes;
        }

        public async Task<(bool Success, string Message)> RestoreDatabaseAsync(IFormFile dbFile)
        {
            if (dbFile == null || dbFile.Length == 0)
            {
                return (false, "El archivo de respaldo no es válido o está vacío.");
            }

            var extension = Path.GetExtension(dbFile.FileName).ToLower();
            if (extension != ".db" && extension != ".sqlite" && extension != ".sqlite3")
            {
                return (false, "Tipo de archivo no válido. Se requiere un archivo .db o .sqlite.");
            }

            var tempFilePath = Path.Combine(Path.GetTempPath(), $"kanban_restore_verify_{Guid.NewGuid()}.db");

            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await dbFile.CopyToAsync(stream);
                }

                // Verify temp DB connection
                var testConnStr = $"Data Source={tempFilePath}";
                using (var conn = new SqliteConnection(testConnStr))
                {
                    await conn.OpenAsync();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM Statuses;";
                    await cmd.ExecuteScalarAsync();
                }

                // If valid, clear pools and overwrite main DB
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                var mainDbPath = GetDbPath();
                File.Copy(tempFilePath, mainDbPath, overwrite: true);

                _logger.LogInformation("Database successfully restored from uploaded file.");
                return (true, "Base de datos restaurada con éxito.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring database file.");
                return (false, $"Error al verificar/restaurar la base de datos: {ex.Message}");
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }
            }
        }
    }
}
