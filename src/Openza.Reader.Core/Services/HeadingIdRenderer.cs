using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System.Net;

namespace Openza.Reader.Services;

public sealed class HeadingIdRenderer : HtmlObjectRenderer<HeadingBlock>
{
    protected override void Write(HtmlRenderer renderer, HeadingBlock obj)
    {
        var id = obj.GetAttributes().Id;
        renderer.EnsureLine();
        renderer.Write("<h").Write(obj.Level.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(id))
        {
            renderer.Write(" id=\"").Write(WebUtility.HtmlEncode(id)).Write("\"");
        }

        renderer.WriteAttributes(obj);
        renderer.Write(">");
        renderer.WriteLeafInline(obj);
        renderer.Write("</h").Write(obj.Level.ToString(System.Globalization.CultureInfo.InvariantCulture)).WriteLine(">");
    }
}

