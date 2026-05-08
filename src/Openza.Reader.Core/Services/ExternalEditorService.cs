using System.ComponentModel;
using System.Diagnostics;

namespace Openza.Reader.Services;

public sealed class ExternalEditorService
{
    public void Open(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        if (TryStart(new ProcessStartInfo(filePath) { UseShellExecute = true, Verb = "edit" }))
        {
            return;
        }

        if (TryStart(new ProcessStartInfo("notepad.exe", Quote(filePath)) { UseShellExecute = true }))
        {
            return;
        }

        _ = TryStart(new ProcessStartInfo(filePath) { UseShellExecute = true });
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

    private static string Quote(string value)
    {
        return $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }
}

