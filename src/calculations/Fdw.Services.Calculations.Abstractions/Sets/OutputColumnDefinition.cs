using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>One column of an output table and how each row's value for it is found.</summary>
public abstract class OutputColumnDefinition
{
    /// <summary>Initializes a new instance of the <see cref="OutputColumnDefinition"/> class.</summary>
    /// <param name="name">The column's name in the output.</param>
    protected OutputColumnDefinition(string name)
    {
        Name = name;
    }

    /// <summary>Gets the column's name in the output.</summary>
    public string Name { get; }

    /// <summary>Gets the kind name this column is stored and chosen by: Copy, Literal or Formula.</summary>
    public abstract string Kind { get; }

    /// <summary>Gets the name of the visible value a Copy column carries through, or null.</summary>
    public virtual string? Source => null;

    /// <summary>Gets the stated value a Literal column holds, or null.</summary>
    public virtual object? Literal => null;

    /// <summary>Gets the formula this column runs, or null when it does not run one.</summary>
    public virtual CalculationNode? Formula => null;

    /// <summary>Gets the name of the visible value this column reads without a formula, or null when it reads none.</summary>
    public virtual string? Reads => null;

    /// <summary>Finds this column's value for one output row.</summary>
    /// <param name="row">The row being built.</param>
    /// <returns>The value.</returns>
    public abstract object? Value(OutputRowContext row);
}
