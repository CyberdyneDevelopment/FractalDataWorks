namespace Fdw.Services.Data.Abstractions.ColumnValues;

/// <summary>One distinct value of a container column (FDW-5666).</summary>
public sealed class ColumnValue
{
    /// <summary>Gets the value, as text.</summary>
    public required string Value { get; init; }

    /// <summary>Gets how many rows of the container carry the value.</summary>
    public required long Rows { get; init; }

    /// <summary>Gets whether the value is one of the values the query names as already chosen.</summary>
    public required bool Chosen { get; init; }
}
