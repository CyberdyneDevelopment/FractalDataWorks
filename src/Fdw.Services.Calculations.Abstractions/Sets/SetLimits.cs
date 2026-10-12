namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>The limits on how large a calculation set's work may grow.</summary>
public sealed class SetLimits
{
    /// <summary>Initializes a new instance of the <see cref="SetLimits"/> class.</summary>
    /// <param name="maxRows">The most rows one output may hold.</param>
    /// <param name="maxLookupRows">The most rows a lookup table may hold.</param>
    public SetLimits(int maxRows, int maxLookupRows)
    {
        MaxRows = maxRows;
        MaxLookupRows = maxLookupRows;
    }

    /// <summary>Gets the most rows one output may hold.</summary>
    public int MaxRows { get; }

    /// <summary>Gets the most rows a lookup table may hold.</summary>
    public int MaxLookupRows { get; }
}
