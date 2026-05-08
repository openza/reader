namespace Openza.Reader.Models;

public sealed record MarkdownRenderResult(
    string HtmlBody,
    IReadOnlyList<TocItem> TocItems,
    string? Title);

