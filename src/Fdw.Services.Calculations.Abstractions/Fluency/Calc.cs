using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.Fluency;

/// <summary>
/// The entry points of the fluent calculation builder: each method makes one node of a calculation tree. Import them with
/// <c>using static</c> and combine them; parentheses are the structure of the tree, so nothing is parsed.
/// </summary>
/// <remarks>
/// A whole number or decimal literal converts to a Constant, so <c>Divide(60, Field("m"))</c> reads as written. Nothing here
/// checks a tree: <see cref="CalculationArrayBuilder.Build"/> and the compiler do, and answer with a coded failure.
/// </remarks>
public static class Calc
{
    /// <summary>The value of an input field of the record.</summary>
    /// <param name="name">The field name.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Field(string name) => CalculationNode.Named("Field", name);

    /// <summary>The value of a result defined earlier in the same array.</summary>
    /// <param name="result">The result name.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Ref(string result) => CalculationNode.Named("Ref", result);

    /// <summary>A numeric literal.</summary>
    /// <param name="value">The literal.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Constant(decimal value) => CalculationNode.FromDecimal(value);

    /// <summary>A true/false literal.</summary>
    /// <param name="value">The literal.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Flag(bool value) => CalculationNode.FromBoolean(value);

    /// <summary>The sum of two or more numbers, added left to right.</summary>
    /// <param name="operands">The numbers.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Add(params CalculationNode[] operands) => CalculationNode.Of("Add", operands);

    /// <summary>The first number less every later number, left to right.</summary>
    /// <param name="operands">The numbers.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Subtract(params CalculationNode[] operands) => CalculationNode.Of("Subtract", operands);

    /// <summary>The product of two or more numbers, multiplied left to right.</summary>
    /// <param name="operands">The numbers.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Multiply(params CalculationNode[] operands) => CalculationNode.Of("Multiply", operands);

    /// <summary>The first number divided by the second; a zero divisor fails with DivisionByZero.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Divide(CalculationNode left, CalculationNode right) => CalculationNode.Of("Divide", left, right);

    /// <summary>The number with its sign reversed.</summary>
    /// <param name="operand">The number.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Negate(CalculationNode operand) => CalculationNode.Of("Negate", operand);

    /// <summary>The first operand that has a value.</summary>
    /// <param name="operands">The operands, at least two.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Coalesce(params CalculationNode[] operands) => CalculationNode.Of("Coalesce", operands);

    /// <summary>The operand, which must have a value; an empty operand fails with RequiredValueEmpty.</summary>
    /// <param name="operand">The operand.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Require(CalculationNode operand) => CalculationNode.Of("Require", operand);

    /// <summary>True when the operand has no value.</summary>
    /// <param name="operand">The operand.</param>
    /// <returns>The node.</returns>
    public static CalculationNode IsEmpty(CalculationNode operand) => CalculationNode.Of("IsEmpty", operand);

    /// <summary>The value when the condition is true, otherwise the other value. Only the chosen branch is evaluated.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="then">The value when the condition is true.</param>
    /// <param name="otherwise">The value when the condition is false.</param>
    /// <returns>The node.</returns>
    public static CalculationNode When(CalculationNode condition, CalculationNode then, CalculationNode otherwise) =>
        CalculationNode.Of("When", condition, then, otherwise);

    /// <summary>The value when the condition is true; otherwise the calculation fails with GuardFailed.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="value">The value.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Guard(CalculationNode condition, CalculationNode value) => CalculationNode.Of("Guard", condition, value);

    /// <summary>True when both numbers have a value and are equal.</summary>
    /// <param name="left">The left number.</param>
    /// <param name="right">The right number.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Equal(CalculationNode left, CalculationNode right) => CalculationNode.Of("Equal", left, right);

    /// <summary>True when both numbers have a value and differ.</summary>
    /// <param name="left">The left number.</param>
    /// <param name="right">The right number.</param>
    /// <returns>The node.</returns>
    public static CalculationNode NotEqual(CalculationNode left, CalculationNode right) => CalculationNode.Of("NotEqual", left, right);

    /// <summary>True when both numbers have a value and the left is greater.</summary>
    /// <param name="left">The left number.</param>
    /// <param name="right">The right number.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Greater(CalculationNode left, CalculationNode right) => CalculationNode.Of("Greater", left, right);

    /// <summary>True when both numbers have a value and the left is not less.</summary>
    /// <param name="left">The left number.</param>
    /// <param name="right">The right number.</param>
    /// <returns>The node.</returns>
    public static CalculationNode GreaterOrEqual(CalculationNode left, CalculationNode right) => CalculationNode.Of("GreaterOrEqual", left, right);

    /// <summary>True when both numbers have a value and the left is less.</summary>
    /// <param name="left">The left number.</param>
    /// <param name="right">The right number.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Less(CalculationNode left, CalculationNode right) => CalculationNode.Of("Less", left, right);

    /// <summary>True when both numbers have a value and the left is not greater.</summary>
    /// <param name="left">The left number.</param>
    /// <param name="right">The right number.</param>
    /// <returns>The node.</returns>
    public static CalculationNode LessOrEqual(CalculationNode left, CalculationNode right) => CalculationNode.Of("LessOrEqual", left, right);

    /// <summary>True when every condition is true.</summary>
    /// <param name="operands">The conditions.</param>
    /// <returns>The node.</returns>
    public static CalculationNode And(params CalculationNode[] operands) => CalculationNode.Of("And", operands);

    /// <summary>True when any condition is true.</summary>
    /// <param name="operands">The conditions.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Or(params CalculationNode[] operands) => CalculationNode.Of("Or", operands);

    /// <summary>The condition reversed.</summary>
    /// <param name="operand">The condition.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Not(CalculationNode operand) => CalculationNode.Of("Not", operand);

    /// <summary>The larger of two or more numbers.</summary>
    /// <param name="operands">The numbers.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Max(params CalculationNode[] operands) => CalculationNode.Of("Max", operands);

    /// <summary>The smaller of two or more numbers.</summary>
    /// <param name="operands">The numbers.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Min(params CalculationNode[] operands) => CalculationNode.Of("Min", operands);

    /// <summary>The number rounded to a stated number of places in a stated midpoint mode.</summary>
    /// <param name="operand">The number.</param>
    /// <param name="places">The decimal places, 0 to 28.</param>
    /// <param name="mode">The midpoint mode: ToEven, AwayFromZero, ToZero, ToNegativeInfinity or ToPositiveInfinity.</param>
    /// <returns>The node.</returns>
    public static CalculationNode Round(CalculationNode operand, int places, string mode) => CalculationNode.Rounded(operand, places, mode);
}
