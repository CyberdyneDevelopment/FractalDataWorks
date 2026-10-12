namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Information about a logging method.
/// </summary>
public sealed class LoggingMethodInfo
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
    /// Gets or sets the event ID.
    /// </summary>
    public required string EventId { get; init; }

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    public required string Level { get; init; }

    /// <summary>
    /// Gets or sets the message template.
    /// </summary>
    public required string Message { get; init; }
}