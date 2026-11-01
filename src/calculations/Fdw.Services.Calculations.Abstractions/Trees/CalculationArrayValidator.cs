using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;
using Fdw.Services.Calculations.Abstractions.TreeResultCodes;

namespace Fdw.Services.Calculations.Abstractions.Trees;

/// <summary>
/// Checks a calculation array before anything is compiled: the shape of the list, every node's operator, arity, members
/// and operand types, the order of the references, and the stated limits.
/// </summary>
/// <remarks>
/// Why one validator for the builder, the compiler and the save-time check: a tree that passes here compiles, so a refusal
/// is always a coded result at build or save time and never a surprise on a record.
/// </remarks>
public static class CalculationArrayValidator
{
    // An operator that states no operand type takes any number, empty or not; a Field under it is read as a possibly empty number.
    private static readonly Type AnyNumber = typeof(decimal?);

    /// <summary>Validates an array against stated limits.</summary>
    /// <param name="array">The array.</param>
    /// <param name="limits">The stated limits.</param>
    /// <returns>Success, or the first refusal.</returns>
    public static IGenericResult Validate(CalculationArrayDefinition array, CalculationLimits limits) =>
        Validate(array, limits, allowDateTimeResults: false);

    /// <summary>Validates an array against stated limits, saying whether a result may be a date-time.</summary>
    /// <param name="array">The array.</param>
    /// <param name="limits">The stated limits.</param>
    /// <param name="allowDateTimeResults">
    /// True when the caller reads a date-time result back through the result's value kind. A caller written before date-times existed
    /// reads every result as a number, so it passes false and a tree whose result is a date-time is refused.
    /// </param>
    /// <returns>Success, or the first refusal.</returns>
    public static IGenericResult Validate(CalculationArrayDefinition array, CalculationLimits limits, bool allowDateTimeResults)
    {
        var shape = CheckShape(array, limits);
        if (shape.IsFailure)
        {
            return shape;
        }

        var context = new CalculationValidationContext(limits);
        foreach (var result in array.Results)
        {
            var outcome = CheckResult(result, context, allowDateTimeResults);
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        return CheckShadowing(context);
    }

    /// <summary>Validates an array that may have date-time results and says what kind of value each of its results is.</summary>
    /// <param name="array">The array.</param>
    /// <param name="limits">The stated limits.</param>
    /// <returns>The kind of every result by name, kept or not, or the first refusal.</returns>
    /// <remarks>
    /// Why it validates and does not compile: a caller that lists many calculations needs only what each yields, and the validator
    /// already knows the type of every result as it checks the tree.
    /// </remarks>
    public static IGenericResult<IReadOnlyDictionary<string, ICalculationValueKind>> ResultKinds(
        CalculationArrayDefinition array, CalculationLimits limits)
    {
        var shape = CheckShape(array, limits);
        if (shape.IsFailure)
        {
            return shape.ToNewResult<IReadOnlyDictionary<string, ICalculationValueKind>>();
        }

        var context = new CalculationValidationContext(limits);
        var kinds = new Dictionary<string, ICalculationValueKind>(StringComparer.Ordinal);
        foreach (var result in array.Results)
        {
            var outcome = CheckResult(result, context, allowDateTimeResults: true);
            if (outcome.IsFailure)
            {
                return outcome.ToNewResult<IReadOnlyDictionary<string, ICalculationValueKind>>();
            }

            kinds[result.Name] = CalculationValueKinds.OfClrType(context.TypeOf(result.Name)!);
        }

        var shadowing = CheckShadowing(context);
        return shadowing.IsFailure
            ? shadowing.ToNewResult<IReadOnlyDictionary<string, ICalculationValueKind>>()
            : GenericResult<IReadOnlyDictionary<string, ICalculationValueKind>>.Success(kinds);
    }

    private static IGenericResult CheckShape(CalculationArrayDefinition array, CalculationLimits limits)
    {
        if (limits.MaxResults < 1 || limits.MaxNodes < 1 || limits.MaxDepth < 1)
        {
            return ListInvalid("the limits must each be positive and stated");
        }

        if (array.Results.Count == 0)
        {
            return ListInvalid($"array '{array.Name}' has no results");
        }

        if (array.Results.Count > limits.MaxResults)
        {
            return GenericResult.Failure(
                CalculationTreeResultCodes.ByName("LimitExceeded"),
                ResultDetails.Create("node", array.Name, "reason", $"the array has {array.Results.Count} results; the limit is {limits.MaxResults}"));
        }

        return CheckNames(array);
    }

    private static IGenericResult CheckNames(CalculationArrayDefinition array)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var result in array.Results)
        {
            if (string.IsNullOrWhiteSpace(result.Name))
            {
                return ListInvalid($"array '{array.Name}' has a result without a name");
            }

            if (!seen.Add(result.Name))
            {
                return GenericResult.Failure(
                    CalculationTreeResultCodes.ByName("DuplicateResultName"),
                    ResultDetails.Create("node", result.Name, "reason", $"'{result.Name}' is defined twice"));
            }
        }

        return GenericResult.Success();
    }

    private static IGenericResult CheckResult(CalculationResultDefinition result, CalculationValidationContext context, bool allowDateTimeResults)
    {
        var type = CheckNode(result.Expression, typeof(decimal), 1, context);
        if (type.IsFailure)
        {
            return type;
        }

        var actual = type.Value!;
        var date = CalculationCoercion.IsDateTime(actual);
        if (!CalculationCoercion.IsNumber(actual) && !(date && allowDateTimeResults))
        {
            var allowed = allowDateTimeResults ? "a number or a date-time" : "a number";
            return Mismatch(result.Expression, $"result '{result.Name}' must be {allowed}, not {(date ? "a date-time" : "a condition")}");
        }

        context.Define(result.Name, date ? typeof(DateTimeOffset) : typeof(decimal));
        return GenericResult.Success();
    }

    private static IGenericResult<Type> CheckNode(CalculationNode node, Type expected, int depth, CalculationValidationContext context)
    {
        var limit = context.Enter(node, depth);
        if (limit.IsFailure)
        {
            return limit.ToNewResult<Type>();
        }

        var option = CalculationNodeTypes.ByName(node.Op);
        if (ReferenceEquals(option, CalculationNodeTypes.NotFound))
        {
            return GenericResult<Type>.Failure(
                CalculationTreeResultCodes.ByName("NodeOperatorUnknown"),
                ResultDetails.Create("node", node.Describe(), "reason", $"'{node.Op}' is not a calculation operator"));
        }

        var structure = CheckStructure(option, node, context);
        return structure.IsFailure
            ? structure.ToNewResult<Type>()
            : CheckOperands(option, node, expected, depth, context);
    }

    private static IGenericResult CheckStructure(ICalculationNodeType option, CalculationNode node, CalculationValidationContext context)
    {
        var count = node.Operands.Count;
        if (count < option.MinOperands || (option.MaxOperands >= 0 && count > option.MaxOperands))
        {
            return GenericResult.Failure(
                CalculationTreeResultCodes.ByName("NodeArityInvalid"),
                ResultDetails.Create("node", node.Describe(), "reason", $"{node.Op} takes {Arity(option)} operand(s), not {count}"));
        }

        return option.Check(node, context);
    }

    private static IGenericResult<Type> CheckOperands(
        ICalculationNodeType option, CalculationNode node, Type expected, int depth, CalculationValidationContext context)
    {
        var types = new List<Type>(node.Operands.Count);
        for (var i = 0; i < node.Operands.Count; i++)
        {
            var required = option.OperandType(i);
            var child = CheckNode(node.Operands[i], required is null ? AnyNumber : required, depth + 1, context);
            if (child.IsFailure)
            {
                return child;
            }

            var compatible = required is null ? CalculationCoercion.IsNumber(child.Value!) : CalculationCoercion.CanCoerce(child.Value!, required);
            if (!compatible)
            {
                return Mismatch(node.Operands[i], $"operand {i + 1} of {node.Describe()} has the wrong type").ToNewResult<Type>();
            }

            types.Add(child.Value!);
        }

        return GenericResult<Type>.Success(option.ResultType(node, expected, types));
    }

    private static IGenericResult CheckShadowing(CalculationValidationContext context)
    {
        foreach (var name in context.Defined)
        {
            if (context.HasField(name))
            {
                return GenericResult.Failure(
                    CalculationTreeResultCodes.ByName("ResultShadowsField"),
                    ResultDetails.Create("node", name, "reason", $"result '{name}' has the name of an input field the array reads"));
            }
        }

        return GenericResult.Success();
    }

    private static string Arity(ICalculationNodeType option) =>
        option.MaxOperands < 0 ? $"at least {option.MinOperands}"
        : option.MinOperands == option.MaxOperands ? option.MinOperands.ToString(System.Globalization.CultureInfo.InvariantCulture)
        : $"{option.MinOperands} to {option.MaxOperands}";

    private static IGenericResult ListInvalid(string reason) =>
        GenericResult.Failure(
            CalculationTreeResultCodes.ByName("ResultListInvalid"),
            ResultDetails.Create("node", "array", "reason", reason));

    private static IGenericResult Mismatch(CalculationNode node, string reason) =>
        GenericResult.Failure(
            CalculationTreeResultCodes.ByName("OperandTypeMismatch"),
            ResultDetails.Create("node", node.Describe(), "reason", reason));
}
