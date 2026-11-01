using System;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// What a split does with a row that meets none of its filters (FDW-1386): stated, never assumed.
/// </summary>
public sealed class FlowUnmatched
{
    private FlowUnmatched(string? output)
    {
        Output = output;
    }

    /// <summary>Gets the output node that takes unmatched rows; null when an unmatched row fails the run.</summary>
    public string? Output { get; }

    /// <summary>An unmatched row fails the run.</summary>
    public static FlowUnmatched Fail { get; } = new(null);

    /// <summary>Unmatched rows go to an output node of their own.</summary>
    /// <param name="output">The output node's name, which must not be one of the filtered outputs.</param>
    /// <returns>The rule.</returns>
    public static FlowUnmatched SendTo(string output)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(output);
        return new FlowUnmatched(output);
    }
}
