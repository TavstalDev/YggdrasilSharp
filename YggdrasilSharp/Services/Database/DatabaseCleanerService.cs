namespace Tavstal.YggdrasilSharp.Services.Database;

/// <summary>
/// A background service that periodically cleans up expired content from the database.
/// </summary>
public class DatabaseCleanerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    private int _cleanupFails;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseCleanerService"/> class.
    /// </summary>
    /// <param name="scopeFactory">The factory to create service scopes.</param>
    /// <param name="logger">The logger instance for logging messages.</param>
    public DatabaseCleanerService(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseCleanerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes the background service, periodically invoking the cleanup process.
    /// </summary>
    /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the background execution.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupExpiredContentAsync(stoppingToken);
            switch (_cleanupFails)
            {
                case > 5:
                    _logger.LogError("Cleanup has failed more than 5 times. Stopping the cleanup service.");
                    return;
                case > 3:
                    _logger.LogWarning("Cleanup has failed more than 3 times. Waiting for 5 minutes before retrying.");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    break;
                case > 0:
                    _logger.LogWarning("Cleanup has failed. Waiting for 1 minute before retrying.");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    break;
                default:
                    await Task.Delay(_cleanupInterval, stoppingToken);
                    break;
            }
        }
    }

    /// <summary>
    /// Cleans up expired content from the database, including user logins, play sessions, and server joins.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the cleanup operation.</returns>
    private async Task CleanupExpiredContentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CustomDbContext>();
            
            // User Logins
            await db.ClearExpiredUserLoginsAsync(cancellationToken: cancellationToken);
            
            // User Play Sessions
            await db.ClearExpiredUserPlaySessionsAsync(cancellationToken: cancellationToken);
            
            // Server joins
            await db.ClearExpiredServerJoinsAsync(cancellationToken: cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            _cleanupFails = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup failed.");
            _cleanupFails++;
        }
    }
}