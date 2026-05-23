using Openza.Reader.Models;
using System.Net;

namespace Openza.Reader.Services;

public sealed class HtmlShellBuilder
{
    private readonly string _readerCss;
    private readonly string _prismCss;
    private readonly string _prismJs;

    public HtmlShellBuilder()
    {
        _readerCss = ReadAsset("reader.css");
        _prismCss = ReadAsset("prism.css");
        _prismJs = ReadAsset("prism.js");
    }

    public string Build(MarkdownRenderResult result, string documentName, string readerTheme, bool allowRemoteImages)
    {
        var title = WebUtility.HtmlEncode(result.Title ?? documentName);
        var theme = WebUtility.HtmlEncode(readerTheme);
        var imgSrc = allowRemoteImages
            ? "file: http: https: data:"
            : "file: data:";
        return $$"""
            <!doctype html>
            <html lang="en" data-reader-theme="{{theme}}">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src {{imgSrc}}; style-src 'unsafe-inline'; script-src 'unsafe-inline'; font-src data:; navigate-to 'none';">
              <title>{{title}}</title>
              <style>
              {{_readerCss}}
              {{_prismCss}}
              </style>
            </head>
            <body>
              <main class="markdown-body">
            {{result.HtmlBody}}
              </main>
              <script>
              {{_prismJs}}
              Prism.highlightAll();
              </script>
            </body>
            </html>
            """;
    }

    private static string ReadAsset(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", name);
        return File.Exists(path)
            ? File.ReadAllText(path)
            : string.Empty;
    }
}
