namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Represents complexity information for a method.
/// </summary>
public sealed class MethodComplexity
{
    /// <summary>
    /// Gets or sets the method name.
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// Gets or sets the cyclomatic complexity.
    /// </summary>
    public required int Complexity { get; init; }

    /// <summary>
    /// Gets or sets the line number.
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Gets or sets whether the complexity exceeds the threshold.
    /// </summary>
    public required bool ExceedsThreshold { get; init; }

    /// <summary>
    /// Gets or sets the containing type name.
    /// </summary>
    public required string ContainingType { get; init; }
}