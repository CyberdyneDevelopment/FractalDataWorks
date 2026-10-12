using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>A value worked out from a table, a total or a lookup, that formulas read by name like any field.</summary>
public abstract class DerivedValueDefinition
{
    /// <summary>Initializes a new instance of the <see cref="DerivedValueDefinition"/> class.</summary>
    /// <param name="name">The name formulas read the value under.</param>
    /// <param name="table">The table the value is taken from.</param>
    protected DerivedValueDefinition(string name, string table)
    {
        Name = name;
        Table = table;
    }

    /// <summary>Gets the name formulas read the value under.</summary>
    public string Name { get; }

    /// <summary>Gets the table the value is taken from.</summary>
    public string Table { get; }

    /// <summary>Gets the kind name this value is stored and chosen by: Sum, Count, Min, Max, Average or Lookup.</summary>
    public abstract string Kind { get; }

    /// <summary>Gets the column that is totalled, or, for a lookup, the column whose value is taken.</summary>
    public abstract string Column { get; }

    /// <summary>Gets a lookup's key column in the searched table, or null for a total.</summary>
    public virtual string? KeyColumn => null;

    /// <summary>Gets the name of the current row's value a lookup searches for, or null for a total.</summary>
    public virtual string? MatchField => null;

    /// <summary>Works the value out for one output row.</summary>
    /// <param name="output">The output asking, for a refusal.</param>
    /// <param name="driver">The row being built.</param>
    /// <param name="scope">The values, tables and limits of this run.</param>
    /// <returns>The value (null when there is nothing to total), or a refusal.</returns>
    public abstract IGenericResult<object?> Compute(string output, DriverRow driver, SetRunScope scope);
}
