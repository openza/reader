using System.Text.RegularExpressions;

namespace Openza.Reader.Services;

public static partial class DocumentStatsCalculator
{
    private const int WordsPerMinute = 200;

    public static int CountWords(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return WordRegex().Count(text);
    }

    public static int EstimateReadMinutes(int wordCount)
    {
        if (wordCount <= 0)
        {
            return 0;
        }

        return Math.Max(1, (int)Math.Ceiling(wordCount / (double)WordsPerMinute));
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:['-][\p{L}\p{N}]+)?", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();
}
