namespace Fdw.Services.Data.Abstractions.DataOptionRows;

/// <summary>One row an add asks for (FDW-5664). Every field is stated; nothing is assumed.</summary>
public sealed class NewDataOptionRow
{
    /// <summary>Gets the column; one of the container's discovered fields.</summary>
    public required string Column { get; init; }

    /// <summary>Gets the value the option applies to; not empty.</summary>
    public required string Value { get; init; }

    /// <summary>Gets the option type, a <c>DataOptions</c> name.</summary>
    public required string OptionType { get; init; }

    /// <summary>Gets the option, a name in the option type's own collection.</summary>
    public required string Option { get; init; }

    /// <summary>Gets whether the row is switched on; it is stated, and a row that does not state it is refused.</summary>
    public required bool? Active { get; init; }
}
