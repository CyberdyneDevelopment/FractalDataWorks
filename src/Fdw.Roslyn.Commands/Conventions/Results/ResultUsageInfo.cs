namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Information about a Result usage.
/// </summary>
public sealed class ResultUsageInfo
{
    /// <summary>
    /// Gets or sets the method name.
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// Gets or sets the containing type.
    /// </summary>
    public required string ContainingType { get; init; }

    /// <summary>
    /// Gets or sets the return type.
    /// </summary>
    public required string ReturnType { get; init; }

    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    public required string Project { get; init; }

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets the line number.
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Gets or sets the count of Success returns.
    /// </summary>
    public required int SuccessReturns { get; init; }

    /// <summary>
    /// Gets or sets the count of Failure returns.
    /// </summary>
    public required int FailureReturns { get; init; }

    /// <summary>
    /// Gets or sets the total count of returns.
    /// </summary>
    public required int TotalReturns { get; init; }
}