using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.Fluency;

/// <summary>
/// Chaining methods on a tree fragment, so a calculation reads left to right: <c>Field("x").Divide(Ref("h"))</c>.
/// </summary>
/// <remarks>
/// Each method makes a new node whose first operand is the fragment it is called on; the fragment itself is never changed.
/// </remarks>
public static class CalculationFluency
{
    /// <summary>Adds the other numbers to this one.</summary>
    /// <param name="first">This fragment.</param>
    /// <param name="others">The numbers to add.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Add(this CalculationNode first, params CalculationNode[] others) => Calc.Add(Join(first, others));

    /// <summary>Subtracts the other numbers from this one, left to right.</summary>
    /// <param name="first">This fragment.</param>
    /// <param name="others">The numbers to subtract.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Subtract(this CalculationNode first, params CalculationNode[] others) => Calc.Subtract(Join(first, others));

    /// <summary>Multiplies this number by the others.</summary>
    /// <param name="first">This fragment.</param>
    /// <param name="others">The numbers to multiply by.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Multiply(this CalculationNode first, params CalculationNode[] others) => Calc.Multiply(Join(first, others));

    /// <summary>Divides this number by another; a zero divisor fails with DivisionByZero.</summary>
    /// <param name="left">This fragment.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Divide(this CalculationNode left, CalculationNode right) => Calc.Divide(left, right);

    /// <summary>Reverses the sign of this number.</summary>
    /// <param name="operand">This fragment.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Negate(this CalculationNode operand) => Calc.Negate(operand);

    /// <summary>Uses the others when this fragment is empty, in order.</summary>
    /// <param name="first">This fragment.</param>
    /// <param name="others">The operands to try next.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Coalesce(this CalculationNode first, params CalculationNode[] others) => Calc.Coalesce(Join(first, others));

    /// <summary>Requires this fragment to have a value.</summary>
    /// <param name="operand">This fragment.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Require(this CalculationNode operand) => Calc.Require(operand);

    /// <summary>The larger of this number and the others.</summary>
    /// <param name="first">This fragment.</param>
    /// <param name="others">The other numbers.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Max(this CalculationNode first, params CalculationNode[] others) => Calc.Max(Join(first, others));

    /// <summary>The smaller of this number and the others.</summary>
    /// <param name="first">This fragment.</param>
    /// <param name="others">The other numbers.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Min(this CalculationNode first, params CalculationNode[] others) => Calc.Min(Join(first, others));

    /// <summary>Rounds this number to a stated number of places in a stated midpoint mode.</summary>
    /// <param name="operand">This fragment.</param>
    /// <param name="places">The decimal places, 0 to 28.</param>
    /// <param name="mode">The midpoint mode.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Round(this CalculationNode operand, int places, string mode) => Calc.Round(operand, places, mode);

    /// <summary>True when this number equals the other.</summary>
    /// <param name="left">This fragment.</param>
    /// <param name="right">The other number.</param>
    /// <returns>The node.</returns>
    public static CalculationNode IsEqualTo(this CalculationNode left, CalculationNode right) => Calc.Equal(left, right);

    /// <summary>True when this number differs from the other.</summary>
    /// <param name="left">This fragment.</param>
    /// <param name="right">The other number.</param>
    /// <returns>The node.</returns>
    public static CalculationNode IsNotEqualTo(this CalculationNode left, CalculationNode right) => Calc.NotEqual(left, right);

    /// <summary>True when this number is greater than the other.</summary>
    /// <param name="left">This fragment.</param>
    /// <param name="right">The other number.</param>
    /// <returns>The node.</returns>
    public static CalculationNode IsGreaterThan(this CalculationNode left, CalculationNode right) => Calc.Greater(left, right);

    /// <summary>True when this number is less than the other.</summary>
    /// <param name="left">This fragment.</param>
    /// <param name="right">The other number.</param>
    /// <returns>The node.</returns>
    public static CalculationNode IsLessThan(this CalculationNode left, CalculationNode right) => Calc.Less(left, right);

    private static CalculationNode[] Join(CalculationNode first, CalculationNode[] others)
    {
        var all = new CalculationNode[others.Length + 1];
        all[0] = first;
        others.CopyTo(all, 1);
        return all;
    }
}
