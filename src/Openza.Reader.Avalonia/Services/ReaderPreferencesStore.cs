using Openza.Reader.Avalonia.Models;
using System.Text.Json;
#if WINDOWS
using Windows.Foundation.Collections;
using Windows.Storage;
#endif

namespace Openza.Reader.Avalonia.Services;

public sealed class ReaderPreferencesStore
{
    private const int RecentFileLimit = 8;
    private static readonly StringComparer FilePathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

    public ReaderPreferencesStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingsPath = Path.Combine(appData, "Openza", "Reader", "settings.json");
    }

    public ReaderPreferences Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
#if WINDOWS
                var migrated = TryMigratePackagedWindowsSettings();
                if (migrated is not null)
                {
                    Save(migrated);
                    return migrated;
                }
#endif
                return new ReaderPreferences();
            }

            return JsonSerializer.Deserialize<ReaderPreferences>(File.ReadAllText(_settingsPath))
                ?? new ReaderPreferences();
        }
        catch (JsonException)
        {
            return new ReaderPreferences();
        }
        catch (IOException)
        {
            return new ReaderPreferences();
        }
    }

    public void Save(ReaderPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        var directory = Path.GetDirectoryName(_settingsPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = _settingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(preferences, _serializerOptions));
        File.Move(temporaryPath, _settingsPath, overwrite: true);
    }

    public void AddRecentFile(ReaderPreferences preferences, string path)
    {
        preferences.RecentFiles = preferences.RecentFiles
            .Where(File.Exists)
            .Where(item => !FilePathComparer.Equals(item, path))
            .Prepend(path)
            .Take(RecentFileLimit)
            .ToList();
        Save(preferences);
    }

#if WINDOWS
    private static ReaderPreferences? TryMigratePackagedWindowsSettings()
    {
        try
        {
            var values = ApplicationData.Current.LocalSettings.Values;
            var foundValue = false;
            var preferences = new ReaderPreferences();

            if (TryReadEnum(values, "DefaultViewMode", out ReaderViewMode viewMode))
            {
                preferences.ViewMode = viewMode;
                foundValue = true;
            }

            if (TryReadEnum(values, "ReaderTheme", out ReaderColorTheme theme))
            {
                preferences.Theme = theme;
                foundValue = true;
            }

            if (values.TryGetValue("RemoteImages", out var remoteImages) && remoteImages is string remoteImageText)
            {
                preferences.AllowRemoteImages = !string.Equals(remoteImageText, "Block", StringComparison.OrdinalIgnoreCase);
                foundValue = true;
            }

            if (values.TryGetValue("ShowDocumentStats", out var showStats) && showStats is bool showDocumentStats)
            {
                preferences.ShowDocumentStats = showDocumentStats;
                foundValue = true;
            }

            if (values.TryGetValue("RecentFiles", out var recentFiles) && recentFiles is string recentFilesJson)
            {
                preferences.RecentFiles = (JsonSerializer.Deserialize<List<string>>(recentFilesJson) ?? [])
                    .Where(File.Exists)
                    .Distinct(FilePathComparer)
                    .Take(RecentFileLimit)
                    .ToList();
                foundValue = true;
            }

            return foundValue ? preferences : null;
        }
        catch (Exception exception) when (exception is InvalidOperationException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException or JsonException)
        {
            // ApplicationData requires package identity. Unpackaged Windows builds simply start with defaults.
            return null;
        }
    }

    private static bool TryReadEnum<T>(IPropertySet values, string key, out T result)
        where T : struct
    {
        if (values.TryGetValue(key, out var value) && value is string text && Enum.TryParse(text, ignoreCase: true, out result))
        {
            return true;
        }

        result = default;
        return false;
    }
#endif

}
