using Openza.Reader.Services;
using Xunit;

namespace Openza.Reader.Tests;

public sealed class HtmlTextHighlighterTests
{
    [Fact]
    public void HighlightsVisibleTextWithoutChangingAttributes()
    {
        var result = HtmlTextHighlighter.HighlightOccurrence(
            "<p title=\"needle\">Needle text</p>",
            "needle",
            0);

        Assert.True(result.Found);
        Assert.Equal(1, result.MatchCount);
        Assert.Contains("title=\"needle\"", result.Html);
        Assert.Contains($"id=\"{HtmlTextHighlighter.MatchElementId}\"", result.Html);
        Assert.Contains(">Needle</span> text", result.Html);
    }

    [Fact]
    public void SelectsAndWrapsOccurrences()
    {
        const string html = "<p>One match</p><p>Another MATCH</p>";

        var second = HtmlTextHighlighter.HighlightOccurrence(html, "match", 1);
        var wrapped = HtmlTextHighlighter.HighlightOccurrence(html, "match", 2);

        Assert.Equal(2, second.MatchCount);
        Assert.Contains("Another <span", second.Html);
        Assert.Equal(0, wrapped.MatchIndex);
        Assert.Contains("One <span", wrapped.Html);
    }

    [Fact]
    public void WrapsNegativeOccurrenceIndexesWithoutOverflow()
    {
        const string html = "<p>match</p><p>match</p><p>match</p>";

        var result = HtmlTextHighlighter.HighlightOccurrence(html, "match", int.MinValue);

        Assert.True(result.Found);
        Assert.InRange(result.MatchIndex, 0, 2);
    }

    [Fact]
    public void HandlesHtmlEncodedSearchText()
    {
        var result = HtmlTextHighlighter.HighlightOccurrence("<p>Fish &amp; chips</p>", "&", 0);

        Assert.True(result.Found);
        Assert.Contains(">&amp;</span>", result.Html);
    }

    [Fact]
    public void DoesNotMatchInsideAnHtmlEntityName()
    {
        const string html = "<p>Fish &amp; chips</p>";

        var result = HtmlTextHighlighter.HighlightOccurrence(html, "amp", 0);

        Assert.False(result.Found);
        Assert.Equal(html, result.Html);
    }

    [Fact]
    public void HighlightsDecodedAngleBracketsWithoutBreakingTheEntity()
    {
        var result = HtmlTextHighlighter.HighlightOccurrence("<p>&lt;safe&gt;</p>", "<safe>", 0);

        Assert.True(result.Found);
        Assert.Contains(">&lt;safe&gt;</span>", result.Html);
    }

    [Fact]
    public void HighlightsVisiblePhraseAcrossInlineMarkup()
    {
        var result = HtmlTextHighlighter.HighlightOccurrence(
            "<p>Hello <strong>world</strong>.</p>",
            "hello world",
            0);

        Assert.True(result.Found);
        Assert.Equal(1, result.MatchCount);
        Assert.Contains("<span id=\"openza-search-match\" class=\"openza-search-match\">Hello </span><strong><span class=\"openza-search-match\">world</span></strong>", result.Html);
    }

    [Fact]
    public void DoesNotJoinTextAcrossBlockBoundaries()
    {
        var result = HtmlTextHighlighter.HighlightOccurrence("<p>Hello</p><p>world</p>", "helloworld", 0);

        Assert.False(result.Found);
    }

    [Fact]
    public void ReturnsOriginalHtmlWhenNoTextMatches()
    {
        const string html = "<a href=\"needle\">Link</a>";

        var result = HtmlTextHighlighter.HighlightOccurrence(html, "needle", 0);

        Assert.False(result.Found);
        Assert.Equal(html, result.Html);
    }
}
