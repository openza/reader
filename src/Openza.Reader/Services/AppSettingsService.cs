using Openza.Reader.Models;
using System.Text.Json;
using Windows.Storage;

namespace Openza.Reader.Services;

public sealed class AppSettingsService
{
    private const string DefaultViewModeKey = "DefaultViewMode";
    private const string ReaderThemeKey = "ReaderTheme";
    private const string RemoteImagesKey = "RemoteImages";
    private const string ShowDocumentStatsKey = "ShowDocumentStats";
    private const string RecentFilesKey = "RecentFiles";
    private const int RecentFileLimit = 8;

    private readonly ApplicationDataContainer _settings = ApplicationData.Current.LocalSettings;

    public ReaderSettings Load()
    {
        return new ReaderSettings
        {
            DefaultViewMode = GetEnum(DefaultViewModeKey, DocumentViewMode.Preview),
            ReaderTheme = GetEnum(ReaderThemeKey, ReaderThemeKind.System),
            RemoteImages = GetEnum(RemoteImagesKey, RemoteImagePolicy.Allow),
            ShowDocumentStats = GetBool(ShowDocumentStatsKey, true)
        };
    }

    public void Save(ReaderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Set(DefaultViewModeKey, settings.DefaultViewMode.ToString());
        Set(ReaderThemeKey, settings.ReaderTheme.ToString());
        Set(RemoteImagesKey, settings.RemoteImages.ToString());
        Set(ShowDocumentStatsKey, settings.ShowDocumentStats);
    }

    public IReadOnlyList<RecentFileItem> LoadRecentFiles()
    {
        var json = GetString(RecentFilesKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var paths = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            return paths
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(RecentFileLimit)
                .Select(path => new RecentFileItem(path))
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public void AddRecentFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var paths = LoadRecentFiles()
            .Select(item => item.Path)
            .Where(path => !string.Equals(path, filePath, StringComparison.OrdinalIgnoreCase))
            .Prepend(filePath)
            .Take(RecentFileLimit)
            .ToList();

        Set(RecentFilesKey, JsonSerializer.Serialize(paths));
    }

    public void ClearRecentFiles()
    {
        _settings.Values.Remove(RecentFilesKey);
    }

    private T GetEnum<T>(string key, T fallback)
        where T : struct
    {
        var value = GetString(key);
        return Enum.TryParse(value, ignoreCase: true, out T result)
            ? result
            : fallback;
    }

    private bool GetBool(string key, bool fallback)
    {
        return _settings.Values.TryGetValue(key, out var value) && value is bool result
            ? result
            : fallback;
    }

    private string? GetString(string key)
    {
        return _settings.Values.TryGetValue(key, out var value)
            ? value as string
            : null;
    }

    private void Set(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _settings.Values.Remove(key);
            return;
        }

        _settings.Values[key] = value;
    }

    private void Set(string key, bool value)
    {
        _settings.Values[key] = value;
    }
}
