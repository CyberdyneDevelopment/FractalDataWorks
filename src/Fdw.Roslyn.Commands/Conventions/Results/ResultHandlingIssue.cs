namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Information about a Result handling issue.
/// </summary>
public sealed class ResultHandlingIssue
{
    /// <summary>
    /// Gets or sets the severity level.
    /// </summary>
    public required string Severity { get; init; }

    /// <summary>
    /// Gets or sets the issue message.
    /// </summary>
    public required string Message { get; init; }

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
    /// Gets or sets the code snippet.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets or sets the method name (for throw issues).
    /// </summary>
    public string? MethodName { get; init; }
}