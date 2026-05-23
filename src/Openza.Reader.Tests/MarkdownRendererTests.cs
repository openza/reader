using Openza.Reader.Services;
using Xunit;

namespace Openza.Reader.Tests;

public sealed class MarkdownRendererTests
{
    private readonly MarkdownRenderer _renderer = new();

    [Fact]
    public void RendersGfmTablesAndTaskLists()
    {
        var result = _renderer.Render("""
# Tasks

| A | B |
|---|---|
| 1 | 2 |

- [x] Done
- [ ] Open
""", TestPath());

        Assert.Contains("<table>", result.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("checkbox", result.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.Single(result.TocItems);
        Assert.Equal("Tasks", result.TocItems[0].Title);
    }

    [Fact]
    public void EscapesRawHtml()
    {
        var result = _renderer.Render("""
            # Unsafe

            <script>alert(1)</script>
            <img src=x onerror=alert(1)>
            """, TestPath());

        Assert.DoesNotContain("<script>", result.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", result.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script", result.HtmlBody, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("[ok](https://example.com)", "https://example.com")]
    [InlineData("[mail](mailto:test@example.com)", "mailto:test@example.com")]
    [InlineData("[bad](javascript:alert(1))", LinkPolicy.BlockedLink)]
    [InlineData("[file](file:///C:/secret.txt)", LinkPolicy.BlockedLink)]
    public void RewritesLinksAccordingToPolicy(string markdown, string expected)
    {
        var result = _renderer.Render(markdown, TestPath());

        Assert.Contains($"href=\"{expected}", result.HtmlBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolvesRelativeImagesToFileUris()
    {
        var result = _renderer.Render("![alt](images/logo.png)", TestPath());

        Assert.Contains("src=\"file:///", result.HtmlBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/images/logo.png", result.HtmlBody.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BlocksRemoteImagesWhenConfigured()
    {
        var result = _renderer.Render("![alt](https://example.com/logo.png)", TestPath(), allowRemoteImages: false);

        Assert.Contains($"src=\"{LinkPolicy.BlockedLink}\"", result.HtmlBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IncludesCodeSpansInHeadingTitles()
    {
        var result = _renderer.Render("## `dotnet test` command", TestPath());

        Assert.Single(result.TocItems);
        Assert.Equal("dotnet test command", result.TocItems[0].Title);
        Assert.Equal("dotnet test command", result.Title);
    }

    [Fact]
    public void CalculatesDocumentStats()
    {
        var result = _renderer.Render("""
# Title

This document has enough words to produce useful statistics.

## Details
""", TestPath());

        Assert.True(result.Stats.WordCount >= 10);
        Assert.Equal(1, result.Stats.EstimatedReadMinutes);
        Assert.Equal(2, result.Stats.HeadingCount);
    }

    private static string TestPath()
    {
        return Path.Combine(Path.GetTempPath(), "openza-reader-test", "README.md");
    }
}
