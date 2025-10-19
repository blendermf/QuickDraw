using QuickDraw.Contracts.Services;
using QuickDraw.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using ApplicationData = Microsoft.Windows.Storage.ApplicationData;

namespace QuickDraw.Services;

class SettingsService : ISettingsService
{
    public Settings? Settings { get; private set; }

    private readonly SemaphoreSlim _ioSemaphore = new(1,1);
    private bool _isInitialized = false;
    private StorageFolder? _dataFolder;

    public async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDataFolder = await StorageFolder.GetFolderFromPathAsync(appDataPath);

            _dataFolder = await appDataFolder.CreateFolderAsync("MFDigitalMedia.QuickDraw", CreationCollisionOption.OpenIfExists);

            _isInitialized = true;

            await ReadSettings();
        }
    }

    public async Task ReadSettings()
    {
        await InitializeAsync();

        await _ioSemaphore.WaitAsync();

        try
        {
            var file = await _dataFolder?.CreateFileAsync("settings.json", CreationCollisionOption.OpenIfExists);

            using var stream = await file.OpenStreamForReadAsync();
            Settings = await JsonSerializer.DeserializeAsync<Settings>(stream);
        }
        catch (JsonException ex)
        {
            Debug.WriteLine(ex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            _ioSemaphore.Release();
        }
    }

    public async Task WriteSettings()
    {
        if (Settings == null)
            return;

        await InitializeAsync();

        await _ioSemaphore.WaitAsync();
        try
        {
            var file = await _dataFolder?.CreateFileAsync("settings.json", CreationCollisionOption.OpenIfExists);

            using var stream = await file.OpenStreamForWriteAsync();
            await JsonSerializer.SerializeAsync<Settings>(stream, Settings);
        }
        catch (JsonException ex)
        {
            Debug.WriteLine(ex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            _ioSemaphore.Release();
        }
    }
}
