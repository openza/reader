namespace Openza.Reader.Models;

public sealed record DocumentStats(
    int WordCount,
    int EstimatedReadMinutes,
    int HeadingCount);

