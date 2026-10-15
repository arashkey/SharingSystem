using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharingSystem.Infrastructure.Persistence;
using SharingSystem.Infrastructure.Storage;

namespace SharingSystem.Infrastructure.Backup;

public interface IDatabaseBackupService
{
    /// <summary>Writes a consistent snapshot of the database, removes old snapshots and returns the snapshot path.</summary>
    Task<string> CreateSnapshotAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates database snapshots with the SQLite online backup API, which is safe while the application is running
/// (unlike copying the database file, which can produce a corrupt copy when a write is in progress).
/// </summary>
public sealed class DatabaseBackupService(
    AppDbContext dbContext,
    DataPathProvider dataPathProvider,
    IOptions<BackupOptions> options,
    TimeProvider timeProvider,
    ILogger<DatabaseBackupService> logger) : IDatabaseBackupService
{
    private const string SnapshotPrefix = "sharing-";
    private const string SnapshotExtension = ".db";
    private const string TemporaryExtension = ".tmp";
    private const string TimestampFormat = "yyyyMMdd-HHmmss";

    public async Task<string> CreateSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var backupDirectory = GetBackupDirectory();
        Directory.CreateDirectory(backupDirectory);

        var timestamp = timeProvider.GetUtcNow().UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture);
        var snapshotPath = Path.Combine(backupDirectory, SnapshotPrefix + timestamp + SnapshotExtension);
        var temporaryPath = snapshotPath + TemporaryExtension;

        try
        {
            await WriteSnapshotAsync(temporaryPath, cancellationToken);
            File.Move(temporaryPath, snapshotPath, overwrite: true);
        }
        catch
        {
            TryDelete(temporaryPath);
            throw;
        }

        DeleteOldSnapshots(backupDirectory);
        logger.LogInformation("Database snapshot written to {Path}", snapshotPath);
        return snapshotPath;
    }

    private async Task WriteSnapshotAsync(string targetPath, CancellationToken cancellationToken)
    {
        var source = (SqliteConnection)dbContext.Database.GetDbConnection();

        // Pooling is disabled so that the snapshot file is released as soon as the connection is closed.
        var targetConnectionString = new SqliteConnectionStringBuilder { DataSource = targetPath, Pooling = false }.ToString();
        await using var target = new SqliteConnection(targetConnectionString);

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await target.OpenAsync(cancellationToken);
            await Task.Run(() => source.BackupDatabase(target), cancellationToken);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private void DeleteOldSnapshots(string backupDirectory)
    {
        // Snapshot names contain a sortable timestamp, so the newest snapshots come first.
        var outdated = Directory.EnumerateFiles(backupDirectory, SnapshotPrefix + "*" + SnapshotExtension)
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
            .Skip(options.Value.RetentionCount);

        foreach (var path in outdated)
        {
            TryDelete(path);
        }
    }

    private string GetBackupDirectory() =>
        Path.GetFullPath(Path.IsPathRooted(options.Value.Path)
            ? options.Value.Path
            : Path.Combine(dataPathProvider.DataPath, options.Value.Path));

    private void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Could not delete {Path}", path);
        }
    }
}
