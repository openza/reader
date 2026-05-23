using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.Tables;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Openza.Reader.Models;
using System.Net;

namespace Openza.Reader.Services;

public sealed class MarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownRenderer()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UsePipeTables()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
            .DisableHtml()
            .Build();
    }

    public MarkdownRenderResult Render(string markdown, string sourcePath, bool allowRemoteImages = true)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        var document = Markdown.Parse(markdown, _pipeline);
        RewriteLinks(document, sourcePath, allowRemoteImages);
        var tocItems = BuildToc(document);
        var html = RenderDocument(document, _pipeline);
        var title = tocItems.FirstOrDefault()?.Title;
        var wordCount = DocumentStatsCalculator.CountWords(PlainText(document));
        var stats = new DocumentStats(
            wordCount,
            DocumentStatsCalculator.EstimateReadMinutes(wordCount),
            tocItems.Count);

        return new MarkdownRenderResult(html, tocItems, title, stats);
    }

    private static string RenderDocument(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        pipeline.Setup(renderer);
        renderer.ObjectRenderers.ReplaceOrAdd<HeadingRenderer>(new HeadingIdRenderer());
        renderer.Render(document);
        writer.Flush();
        return writer.ToString();
    }

    private static IReadOnlyList<TocItem> BuildToc(MarkdownDocument document)
    {
        return document.Descendants<HeadingBlock>()
            .Select(block =>
            {
                var id = block.GetAttributes().Id;
                var title = PlainText(block.Inline);
                return string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title)
                    ? null
                    : new TocItem(id, WebUtility.HtmlDecode(title), block.Level);
            })
            .Where(item => item is not null)
            .Cast<TocItem>()
            .ToList();
    }

    private static void RewriteLinks(MarkdownDocument document, string sourcePath, bool allowRemoteImages)
    {
        var sourceDirectory = Path.GetDirectoryName(sourcePath) ?? Environment.CurrentDirectory;
        foreach (var link in document.Descendants<LinkInline>())
        {
            if (string.IsNullOrWhiteSpace(link.Url))
            {
                continue;
            }

            link.Url = LinkPolicy.Rewrite(link.Url, sourceDirectory, link.IsImage, allowRemoteImages);
        }
    }

    private static string PlainText(MarkdownDocument document)
    {
        var parts = document
            .Descendants()
            .Select(part => part switch
            {
                LiteralInline literal => literal.Content.ToString(),
                CodeInline code => code.Content,
                LineBreakInline => " ",
                _ => string.Empty
            });

        return string.Join(" ", parts).Trim();
    }

    private static string PlainText(ContainerInline? inline)
    {
        if (inline is null)
        {
            return string.Empty;
        }

        var parts = inline
            .Descendants()
            .Select(part => part switch
            {
                LiteralInline literal => literal.Content.ToString(),
                CodeInline code => code.Content,
                LineBreakInline => " ",
                _ => string.Empty
            });

        return string.Join(string.Empty, parts).Trim();
    }
}
