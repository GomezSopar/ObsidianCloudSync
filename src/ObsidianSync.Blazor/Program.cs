using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.EntityFrameworkCore;
using ObsidianSync.Application.Interfaces;
using ObsidianSync.Application.Services;
using ObsidianSync.Blazor.Components;
using ObsidianSync.Blazor.Workers;
using ObsidianSync.Domain.Repositories;
using ObsidianSync.Infrastructure.GoogleDrive;
using ObsidianSync.Infrastructure.LocalStorage;
using ObsidianSync.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var syncCfg = builder.Configuration.GetSection("ObsidianSync");
var vaultName = syncCfg["VaultName"] ?? "Default Vault";
var localPath = syncCfg["LocalPath"] ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Obsidian");
var remoteFolderId = syncCfg["RemoteFolderId"] ?? "root";
var credPath = syncCfg["CredentialsPath"] ?? "credentials.json";

builder.Services.AddDbContext<VaultDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("SyncDb") ?? "Data Source=obsidian_sync_state.db"));

builder.Services.AddScoped<IVaultRepository>(sp =>
    new EfVaultRepository(
        sp.GetRequiredService<VaultDbContext>(),
        vaultName,
        localPath,
        remoteFolderId));

builder.Services.AddSingleton<DriveService>(_ =>
{
    if (File.Exists(credPath))
    {
        using var stream = new FileStream(credPath, FileMode.Open, FileAccess.Read);
        var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(stream).Secrets,
            new[] { DriveService.Scope.DriveFile },
            "kike-obsidian-sync",
            CancellationToken.None,
            new FileDataStore("token.store", true)).GetAwaiter().GetResult();

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "ObsidianDriveSync"
        });
    }

    return new DriveService(new BaseClientService.Initializer
    {
        ApplicationName = "ObsidianDriveSync"
    });
});

builder.Services.AddSingleton<ILocalVaultStorage, LocalFileSystemVaultStorage>();
builder.Services.AddSingleton<ICloudStorageProvider, GoogleDriveStorageProvider>();
builder.Services.AddScoped<VaultSyncOrchestrator>();

builder.Services.AddHostedService<BackgroundSyncWorker>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
