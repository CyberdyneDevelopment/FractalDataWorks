namespace Fdw.Data.Abstractions;

/// <summary>
/// A 1-based, inclusive line range that scopes a <see cref="RawText"/> read to a subset of lines.
/// </summary>
/// <param name="StartLine">First line to return (1-based, inclusive).</param>
/// <param name="EndLine">Last line to return (1-based, inclusive).</param>
/// <remarks>
/// Line numbers follow the Roslyn convention: lines are 1-based and both ends of the range
/// are inclusive. A single-line range has <c>StartLine == EndLine</c>.
/// </remarks>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record RawTextLineRange(int StartLine, int EndLine);
