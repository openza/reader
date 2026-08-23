using System.Net;
using System.Text;

namespace Openza.Reader.Services;

public static class HtmlTextHighlighter
{
    public const string MatchElementId = "openza-search-match";

    public static HtmlHighlightResult HighlightOccurrence(string html, string query, int occurrenceIndex)
    {
        ArgumentNullException.ThrowIfNull(html);
        if (string.IsNullOrWhiteSpace(query))
        {
            return new HtmlHighlightResult(html, false, 0, 0);
        }

        var matches = FindTextMatches(html, query);

        if (matches.Count == 0)
        {
            return new HtmlHighlightResult(html, false, 0, 0);
        }

        var normalizedIndex = occurrenceIndex % matches.Count;
        if (normalizedIndex < 0)
        {
            normalizedIndex += matches.Count;
        }
        var highlighted = new StringBuilder(html);
        var ranges = matches[normalizedIndex].Ranges;
        for (var rangeIndex = ranges.Count - 1; rangeIndex >= 0; rangeIndex--)
        {
            var range = ranges[rangeIndex];
            highlighted.Insert(range.End, "</span>");
            highlighted.Insert(
                range.Start,
                rangeIndex == 0
                    ? $"<span id=\"{MatchElementId}\" class=\"openza-search-match\">"
                    : "<span class=\"openza-search-match\">");
        }

        return new HtmlHighlightResult(highlighted.ToString(), true, normalizedIndex, matches.Count);
    }

    private static List<TextMatch> FindTextMatches(string html, string query)
    {
        var visibleText = new StringBuilder(html.Length);
        var sourceRanges = new List<SourceRange>(html.Length);
        var position = 0;
        while (position < html.Length)
        {
            var tagStart = html.IndexOf('<', position);
            var textEnd = tagStart < 0 ? html.Length : tagStart;
            AppendVisibleText(html, position, textEnd, visibleText, sourceRanges);
            if (tagStart < 0)
            {
                break;
            }

            var tagEnd = FindTagEnd(html, tagStart + 1);
            if (tagEnd >= 0 && IsBlockBoundary(html, tagStart, tagEnd))
            {
                visibleText.Append('\n');
                sourceRanges.Add(SourceRange.NoSource);
            }
            position = tagEnd < 0 ? html.Length : tagEnd + 1;
        }

        return FindMatches(visibleText.ToString(), sourceRanges, query);
    }

    private static List<TextMatch> FindMatches(
        string visibleText,
        IReadOnlyList<SourceRange> sourceRanges,
        string query)
    {
        var matches = new List<TextMatch>();
        var searchFrom = 0;
        while (searchFrom < visibleText.Length)
        {
            var match = visibleText.IndexOf(query, searchFrom, StringComparison.OrdinalIgnoreCase);
            if (match < 0)
            {
                break;
            }

            var matchedSources = sourceRanges.Skip(match).Take(query.Length).ToList();
            if (matchedSources.All(range => range.HasSource))
            {
                var groupedRanges = new List<SourceRange>();
                foreach (var sourceRange in matchedSources)
                {
                    if (groupedRanges.Count == 0)
                    {
                        groupedRanges.Add(sourceRange);
                        continue;
                    }

                    var previous = groupedRanges[^1];
                    if (sourceRange.Start <= previous.End)
                    {
                        groupedRanges[^1] = new SourceRange(previous.Start, Math.Max(previous.End, sourceRange.End));
                    }
                    else
                    {
                        groupedRanges.Add(sourceRange);
                    }
                }

                matches.Add(new TextMatch(groupedRanges));
            }

            searchFrom = match + Math.Max(1, query.Length);
        }

        return matches;
    }

    private static void AppendVisibleText(
        string html,
        int segmentStart,
        int segmentEnd,
        StringBuilder visibleText,
        ICollection<SourceRange> sourceRanges)
    {
        for (var sourceIndex = segmentStart; sourceIndex < segmentEnd;)
        {
            if (html[sourceIndex] == '&'
                && TryDecodeEntity(html, sourceIndex, segmentEnd, out var decoded, out var entityEnd))
            {
                visibleText.Append(decoded);
                for (var decodedIndex = 0; decodedIndex < decoded.Length; decodedIndex++)
                {
                    sourceRanges.Add(new SourceRange(sourceIndex, entityEnd));
                }
                sourceIndex = entityEnd;
                continue;
            }

            visibleText.Append(html[sourceIndex]);
            sourceRanges.Add(new SourceRange(sourceIndex, sourceIndex + 1));
            sourceIndex++;
        }
    }

    private static bool IsBlockBoundary(string html, int tagStart, int tagEnd)
    {
        var nameStart = tagStart + 1;
        if (nameStart < tagEnd && html[nameStart] == '/')
        {
            nameStart++;
        }

        while (nameStart < tagEnd && char.IsWhiteSpace(html[nameStart]))
        {
            nameStart++;
        }

        var nameEnd = nameStart;
        while (nameEnd < tagEnd && (char.IsLetterOrDigit(html[nameEnd]) || html[nameEnd] == '-'))
        {
            nameEnd++;
        }

        var tagName = html[nameStart..nameEnd];
        return tagName.Equals("br", StringComparison.OrdinalIgnoreCase)
            || tagName.Equals("p", StringComparison.OrdinalIgnoreCase)
            || tagName.Equals("div", StringComparison.OrdinalIgnoreCase)
            || tagName.Equals("li", StringComparison.OrdinalIgnoreCase)
            || tagName.Equals("blockquote", StringComparison.OrdinalIgnoreCase)
            || tagName.Equals("pre", StringComparison.OrdinalIgnoreCase)
            || tagName.Equals("tr", StringComparison.OrdinalIgnoreCase)
            || (tagName.Length == 2 && tagName[0] is 'h' or 'H' && tagName[1] is >= '1' and <= '6');
    }

    private static bool TryDecodeEntity(
        string html,
        int entityStart,
        int segmentEnd,
        out string decoded,
        out int entityEnd)
    {
        var semicolon = html.IndexOf(';', entityStart + 1, segmentEnd - entityStart - 1);
        if (semicolon > entityStart && semicolon - entityStart <= 32)
        {
            var encoded = html[entityStart..(semicolon + 1)];
            decoded = WebUtility.HtmlDecode(encoded);
            if (!string.Equals(decoded, encoded, StringComparison.Ordinal))
            {
                entityEnd = semicolon + 1;
                return true;
            }
        }

        decoded = string.Empty;
        entityEnd = entityStart;
        return false;
    }

    private static int FindTagEnd(string html, int start)
    {
        var quote = '\0';
        for (var index = start; index < html.Length; index++)
        {
            var character = html[index];
            if (quote != '\0')
            {
                if (character == quote)
                {
                    quote = '\0';
                }
                continue;
            }

            if (character is '\'' or '"')
            {
                quote = character;
            }
            else if (character == '>')
            {
                return index;
            }
        }

        return -1;
    }

    private sealed record TextMatch(IReadOnlyList<SourceRange> Ranges);

    private sealed record SourceRange(int Start, int End)
    {
        public static SourceRange NoSource { get; } = new(-1, -1);

        public bool HasSource => Start >= 0;
    }
}

public sealed record HtmlHighlightResult(string Html, bool Found, int MatchIndex, int MatchCount);
