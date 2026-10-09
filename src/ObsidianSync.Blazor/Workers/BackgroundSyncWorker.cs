namespace ObsidianSync.Blazor.Workers;

using ObsidianSync.Application.Services;
using ObsidianSync.Domain.Repositories;

public class BackgroundSyncWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BackgroundSyncWorker> _logger;
    private readonly TimeSpan _period;

    public BackgroundSyncWorker(
        IServiceProvider services,
        IConfiguration config,
        ILogger<BackgroundSyncWorker> logger)
    {
        _services = services;
        _logger = logger;
        var secs = config.GetValue<int>("ObsidianSync:SyncIntervalSeconds", 120);
        _period = TimeSpan.FromSeconds(secs);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_period);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _services.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IVaultRepository>();
                var orchestrator = scope.ServiceProvider.GetRequiredService<VaultSyncOrchestrator>();

                var vault = await repo.GetOrCreateDefaultAsync(stoppingToken);
                var summary = await orchestrator.RunSyncAsync(vault, stoppingToken);

                _logger.LogInformation(
                    "Sync auto OK -> Subidos: {Up}, Descargados: {Down}, Borrados: {Del}, Conflictos: {Conf}",
                    summary.Uploaded, summary.Downloaded, summary.Deleted, summary.Conflicts);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo en el worker de sincronizacion automatica.");
            }
        }
    }
}
