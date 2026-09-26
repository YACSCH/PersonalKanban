namespace PersonalKanban.Services
{
    public interface IBackupService
    {
        Task<byte[]> GetDatabaseBackupAsync();
        Task<(bool Success, string Message)> RestoreDatabaseAsync(IFormFile dbFile);
    }
}
