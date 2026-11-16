namespace Fdw.Services.Data.Abstractions.DataOptionRows;

/// <summary>What <see cref="IDataOptionRows.List"/> is asked (FDW-5664).</summary>
public sealed class DataOptionRowsQuery
{
    /// <summary>Gets the container whose rows are listed.</summary>
    public required DataOptionRowsContainer Target { get; init; }

    /// <summary>Gets the column whose rows are listed; null lists every column's.</summary>
    public string? Column { get; init; }

    /// <summary>Gets how many rows to pass over, as the platform's page settings sized it.</summary>
    public required int Skip { get; init; }

    /// <summary>Gets how many rows the page holds, as the platform's page settings sized it.</summary>
    public required int Take { get; init; }
}
