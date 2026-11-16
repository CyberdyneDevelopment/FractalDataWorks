namespace Fdw.Services.Data.Abstractions.DataOptionRows;

/// <summary>What <see cref="IDataOptionRows.Change"/> is asked (FDW-5664): only the fields stated change.</summary>
public sealed class DataOptionRowChange
{
    /// <summary>Gets the row.</summary>
    public required DataOptionRowKey Key { get; init; }

    /// <summary>Gets the new option, a name in the row's own option type's collection; null leaves it.</summary>
    public string? Option { get; init; }

    /// <summary>Gets whether the row is switched on; null leaves it.</summary>
    public bool? Active { get; init; }

    /// <summary>Gets the new value; null leaves it, and an empty one is refused.</summary>
    public string? Value { get; init; }
}
