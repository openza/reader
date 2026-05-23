namespace Openza.Reader.Services;

public static class LinkPolicy
{
    public const string BlockedLink = "#blocked-link";

    public static string Rewrite(string url, string sourceDirectory, bool isImage, bool allowRemoteImages = true)
    {
        if (url.StartsWith('#'))
        {
            return url;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
        {
            return RewriteAbsolute(absolute, isImage, allowRemoteImages);
        }

        if (isImage)
        {
            var imagePath = Path.GetFullPath(Path.Combine(sourceDirectory, Uri.UnescapeDataString(url)));
            return new Uri(imagePath).AbsoluteUri;
        }

        return BlockedLink;
    }

    private static string RewriteAbsolute(Uri uri, bool isImage, bool allowRemoteImages)
    {
        if (uri.Scheme is "http" or "https")
        {
            return !isImage || allowRemoteImages
                ? uri.AbsoluteUri
                : BlockedLink;
        }

        if (!isImage && uri.Scheme == "mailto")
        {
            return uri.AbsoluteUri;
        }

        if (isImage && uri.Scheme == "file")
        {
            return uri.AbsoluteUri;
        }

        return BlockedLink;
    }
}
