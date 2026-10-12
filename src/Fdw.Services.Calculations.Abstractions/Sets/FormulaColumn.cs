using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>A column whose value is a formula's result, an exact number.</summary>
public sealed class FormulaColumn : OutputColumnDefinition
{
    private readonly CalculationNode _formula;

    /// <summary>Initializes a new instance of the <see cref="FormulaColumn"/> class.</summary>
    /// <param name="name">The column's name in the output.</param>
    /// <param name="formula">The formula.</param>
    public FormulaColumn(string name, CalculationNode formula)
        : base(name)
    {
        _formula = formula;
    }

    /// <inheritdoc />
    public override string Kind => "Formula";

    /// <inheritdoc />
    public override CalculationNode? Formula => _formula;

    /// <inheritdoc />
    public override object? Value(OutputRowContext row) => row.Computed[Name];
}
