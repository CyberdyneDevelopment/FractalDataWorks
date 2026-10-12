using System.Diagnostics.CodeAnalysis;

namespace Fdw.Commands.Abstractions;

/// <summary>
/// Represents the estimated cost of executing a command.
/// </summary>
/// <remarks>
/// Cost estimates help with query planning, optimization, and resource allocation.
/// Values are relative and can be used for comparison between different execution plans.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class CommandCostEstimate
{
    /// <summary>
    /// Gets or sets the estimated number of rows affected or returned.
    /// </summary>
    public long EstimatedRows { get; init; }

    /// <summary>
    /// Gets or sets the estimated execution time in milliseconds.
    /// </summary>
    public int EstimatedTimeMs { get; init; }

    /// <summary>
    /// Gets or sets the estimated memory usage in bytes.
    /// </summary>
    public long EstimatedMemoryBytes { get; init; }

    /// <summary>
    /// Gets or sets the estimated network transfer size in bytes.
    /// </summary>
    public long EstimatedNetworkBytes { get; init; }

    /// <summary>
    /// Gets or sets the estimated I/O operations.
    /// </summary>
    public int EstimatedIoOperations { get; init; }

    /// <summary>
    /// Gets or sets the complexity score (1-100).
    /// </summary>
    /// <value>Higher values indicate more complex operations.</value>
    public int ComplexityScore { get; init; }

    /// <summary>
    /// Gets or sets whether the estimate is based on statistics.
    /// </summary>
    /// <value>True if statistics were available, false if using heuristics.</value>
    public bool IsStatisticsBased { get; init; }

    /// <summary>
    /// Gets or sets the confidence level of the estimate (0-1).
    /// </summary>
    /// <value>1.0 indicates high confidence, 0.0 indicates pure speculation.</value>
    public double Confidence { get; init; }

    /// <summary>
    /// Creates a minimal cost estimate for simple operations.
    /// </summary>
    public static CommandCostEstimate Minimal => new()
    {
        EstimatedRows = 1,
        EstimatedTimeMs = 1,
        EstimatedMemoryBytes = 1024,
        EstimatedNetworkBytes = 256,
        EstimatedIoOperations = 1,
        ComplexityScore = 1,
        IsStatisticsBased = false,
        Confidence = 0.5
    };

    /// <summary>
    /// Creates an unknown cost estimate when calculation isn't possible.
    /// </summary>
    public static CommandCostEstimate Unknown => new()
    {
        EstimatedRows = -1,
        EstimatedTimeMs = -1,
        EstimatedMemoryBytes = -1,
        EstimatedNetworkBytes = -1,
        EstimatedIoOperations = -1,
        ComplexityScore = -1,
        IsStatisticsBased = false,
        Confidence = 0.0
    };
}