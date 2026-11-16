using System;

namespace Fdw.Services.Data.Abstractions.DataOptionRows;

/// <summary>One option row of a container, by id (FDW-5664).</summary>
public sealed class DataOptionRowKey
{
    /// <summary>Gets the container the row belongs to.</summary>
    public required DataOptionRowsContainer Target { get; init; }

    /// <summary>Gets the row's id.</summary>
    public required Guid Id { get; init; }
}
