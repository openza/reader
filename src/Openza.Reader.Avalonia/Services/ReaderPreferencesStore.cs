using Openza.Reader.Avalonia.Models;
using System.Text.Json;

namespace Openza.Reader.Avalonia.Services;

public sealed class ReaderPreferencesStore
{
    private const int RecentFileLimit = 8;
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

    public ReaderPreferencesStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _settingsPath = Path.Combine(appData, "Openza", "Reader", "settings.json");
    }

    public ReaderPreferences Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
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
            .Where(item => !string.Equals(item, path, StringComparison.Ordinal))
            .Prepend(path)
            .Take(RecentFileLimit)
            .ToList();
        Save(preferences);
    }

}
