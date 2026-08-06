namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Information about a throw statement.
/// </summary>
public sealed class ThrowStatementInfo
{
    /// <summary>
    /// Gets or sets the exception type.
    /// </summary>
    public required string ExceptionType { get; init; }

    /// <summary>
    /// Gets or sets the exception category.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Gets or sets the method name.
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// Gets or sets the containing type.
    /// </summary>
    public required string ContainingType { get; init; }

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
    /// Gets or sets whether this is a Result pattern candidate.
    /// </summary>
    public required bool IsResultCandidate { get; init; }
}