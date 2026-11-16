using System;

namespace Fdw.Services.Data.Abstractions.DataOptionRows;

/// <summary>One stored option row: what an option type and option do to one value of one column (FDW-5664).</summary>
public sealed class DataOptionRow
{
    /// <summary>Gets the row's id.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the data store the column's container is in.</summary>
    public required string DataStore { get; init; }

    /// <summary>Gets the container the column belongs to.</summary>
    public required string Container { get; init; }

    /// <summary>Gets the column.</summary>
    public required string Column { get; init; }

    /// <summary>Gets the value the option applies to.</summary>
    public required string Value { get; init; }

    /// <summary>Gets the option type, a <c>DataOptions</c> name.</summary>
    public required string OptionType { get; init; }

    /// <summary>Gets the option, a name in the option type's own collection.</summary>
    public required string Option { get; init; }

    /// <summary>Gets whether the row is switched on.</summary>
    public required bool Active { get; init; }
}
