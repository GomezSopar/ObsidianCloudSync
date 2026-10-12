# Obsidian Cloud Sync (.NET 8 + Blazor + Google Drive)

Herramienta personal para sincronizar mi boveda de **Obsidian** (apuntes de la UOC y notas del trabajo) entre el PC de sobremesa y el portatil usando **Google Drive API v3** como puente, con un panel en **Blazor Server** para vigilar el estado y forzar sincronizaciones.

## Por que lo hice

Queria mantener mis notas `.md` sincronizadas entre dispositivos sin depender de clientes pesados en segundo plano que bloqueasen el archivo `.obsidian/workspace.json` cada vez que cambiaba de pestana, y de paso practicar una arquitectura limpia separando bien el dominio del acceso a Drive y SQLite.

## Stack y estructura

- **`ObsidianSync.Domain`**: Entidad raiz `Vault`, calculo de decisiones de sincronizacion (`EvaluateFileSync`) y deteccion de conflictos por hash MD5.
- **`ObsidianSync.Application`**: Orquestador (`VaultSyncOrchestrator`) e interfaces de almacenamiento.
- **`ObsidianSync.Infrastructure`**:
  - Cliente de **Google Drive API v3** con OAuth2 (`credentials.json`).
  - Lector de archivos locales usando `FileShare.ReadWrite` (para no chocar con Obsidian cuando esta abierto en Windows) y filtro de archivos temporales.
  - Persistencia del estado de sincronizacion en **SQLite con Entity Framework Core**.
- **`ObsidianSync.Blazor`**: Dashboard web + `BackgroundSyncWorker` con `PeriodicTimer`.
- **`ObsidianSync.UnitTests`**: Tests unitarios con **xUnit** para validar las reglas de sincronizacion y conflictos.

## Como arrancarlo en local

1. Descarga tu `credentials.json` de Google Cloud Console (habilitando *Google Drive API*) y dejalo en `src/ObsidianSync.Blazor/`.
2. Ajusta la ruta de tu boveda (`LocalPath`) y el ID de la carpeta de Drive (`RemoteFolderId`) en `appsettings.json`.
3. Ejecuta los tests y levanta el panel:
   ```powershell
   dotnet test
   dotnet run --project src/ObsidianSync.Blazor/ObsidianSync.Blazor.csproj
