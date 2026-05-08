namespace Openza.Reader.Services;

public static class AppLog
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Openza.Reader");

    private static readonly string LogPath = Path.Combine(LogDirectory, "startup.log");

    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            File.AppendAllText(LogPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // Startup logging must never prevent the app from launching.
        }
    }

    public static void Write(Exception exception)
    {
        Write(exception.ToString());
    }
}

