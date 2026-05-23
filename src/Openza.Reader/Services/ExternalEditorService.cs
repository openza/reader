using System.ComponentModel;
using System.Diagnostics;
using Windows.System;
using Windows.Storage;

namespace Openza.Reader.Services;

public sealed class ExternalEditorService
{
    public async Task<bool> OpenWithPickerAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        var pickerShown = await TryLaunchFilePickerAsync(filePath);
        if (pickerShown.HasValue)
        {
            return pickerShown.Value;
        }

        if (TryStart(new ProcessStartInfo(filePath) { UseShellExecute = true, Verb = "edit" }))
        {
            return true;
        }

        return TryStart(new ProcessStartInfo(filePath) { UseShellExecute = true });
    }

    private static async Task<bool?> TryLaunchFilePickerAsync(string filePath)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(filePath);
            var options = new LauncherOptions
            {
                DisplayApplicationPicker = true
            };

            return await Launcher.LaunchFileAsync(file, options);
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool TryStart(ProcessStartInfo info)
    {
        try
        {
            using var process = Process.Start(info);
            return process is not null;
        }
        catch (Win32Exception)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
