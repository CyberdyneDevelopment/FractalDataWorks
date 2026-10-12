namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Event ID range information.
/// </summary>
public sealed class EventIdRange
{
    /// <summary>
    /// Gets or sets the minimum event ID.
    /// </summary>
    public required int Min { get; init; }

    /// <summary>
    /// Gets or sets the maximum event ID.
    /// </summary>
    public required int Max { get; init; }

    /// <summary>
    /// Gets or sets the count of event IDs.
    /// </summary>
    public required int Count { get; init; }
}