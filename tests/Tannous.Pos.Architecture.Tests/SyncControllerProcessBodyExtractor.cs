namespace Tannous.Pos.Architecture.Tests;

/// <summary>
/// Parses <c>SyncController</c> <c>Process*</c> method bodies for source governance tests.
/// Supports both <c>private async Task&lt;OpResultDto&gt;</c> and <c>private Task&lt;OpResultDto&gt;</c> declarations.
/// </summary>
internal static class SyncControllerProcessBodyExtractor
{
    public const string ProcessMethodDeclarationPattern = @"private\s+(async\s+)?Task<OpResultDto>\s+Process(\w+)\s*\(";

    public static string ExtractProcessBody(string text, string proc)
    {
        var needles = new[]
        {
            $"private async Task<OpResultDto> Process{proc}",
            $"private Task<OpResultDto> Process{proc}"
        };
        var start = int.MaxValue;
        foreach (var needle in needles)
        {
            var idx = text.IndexOf(needle, StringComparison.Ordinal);
            if (idx >= 0 && idx < start)
                start = idx;
        }

        if (start == int.MaxValue)
            return string.Empty;

        var brace = text.IndexOf('{', start);
        if (brace < 0)
            return string.Empty;

        var next = IndexOfNextProcessMethod(text, brace + 1);
        var end = next >= 0 ? next : text.Length;
        return text.Substring(brace, end - brace);
    }

    private static int IndexOfNextProcessMethod(string text, int searchFrom)
    {
        var asyncIdx = text.IndexOf("private async Task<OpResultDto> Process", searchFrom, StringComparison.Ordinal);
        var syncIdx = text.IndexOf("private Task<OpResultDto> Process", searchFrom, StringComparison.Ordinal);
        if (asyncIdx < 0)
            return syncIdx;
        if (syncIdx < 0)
            return asyncIdx;
        return Math.Min(asyncIdx, syncIdx);
    }
}
