namespace Fdw.Services.Calculations.Abstractions.Trees;

/// <summary>
/// One named result of a calculation array: its name, its tree, and whether the run writes it out.
/// </summary>
public sealed class CalculationResultDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationResultDefinition"/> class.
    /// </summary>
    /// <param name="name">The result name; later results reach this one with a Ref node.</param>
    /// <param name="expression">The tree that computes the result.</param>
    /// <param name="emit">True when the run writes the result out; false for an intermediate result that only later results use.</param>
    public CalculationResultDefinition(string name, CalculationNode expression, bool emit)
    {
        Name = name;
        Expression = expression;
        Emit = emit;
    }

    /// <summary>Gets the result name.</summary>
    public string Name { get; }

    /// <summary>Gets the tree that computes the result.</summary>
    public CalculationNode Expression { get; }

    /// <summary>Gets a value indicating whether the run writes the result out.</summary>
    public bool Emit { get; }
}
