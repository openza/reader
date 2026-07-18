using System.Security.Cryptography;
using System.Text;

namespace Openza.Reader.Services;

public sealed class TempHtmlDocumentStore
{
    private readonly string _cacheDirectory;

    public TempHtmlDocumentStore(string cacheRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheRoot);
        _cacheDirectory = Path.Combine(cacheRoot, "RenderCache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<Uri> WriteAsync(string sourcePath, string html)
    {
        var fileName = $"{Hash(sourcePath)}.html";
        var path = Path.Combine(_cacheDirectory, fileName);
        await File.WriteAllTextAsync(path, html, Encoding.UTF8);
        return new Uri(path);
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes)[..24].ToLowerInvariant();
    }
}
