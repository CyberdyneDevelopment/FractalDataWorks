using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Tests.CalculationTrees;

/// <summary>
/// A direct evaluation of a calculation array, written separately from the compiler: it walks the tree with plain decimal
/// arithmetic. The equivalence test compares the compiled delegate against it on random records.
/// </summary>
internal sealed class ReferenceEvaluator
{
    private readonly Dictionary<string, object?> _record;
    private readonly Dictionary<string, decimal> _results = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Func<CalculationNode, decimal?>> _numbers;
    private readonly Dictionary<string, Func<CalculationNode, bool>> _flags;

    private ReferenceEvaluator(Dictionary<string, object?> record)
    {
        _record = record;
        _numbers = new Dictionary<string, Func<CalculationNode, decimal?>>(StringComparer.Ordinal)
        {
            ["Field"] = node => ReadNumber(node.Name!),
            ["Constant"] = node => node.Value,
            ["Ref"] = node => _results[node.Name!],
            ["Add"] = node => Fold(node, (left, right) => left + right),
            ["Subtract"] = node => Fold(node, (left, right) => left - right),
            ["Multiply"] = node => Fold(node, (left, right) => left * right),
            ["Max"] = node => Fold(node, Math.Max),
            ["Min"] = node => Fold(node, Math.Min),
            ["Divide"] = Divide,
            ["Negate"] = node => -Number(node.Operands[0]),
            ["Coalesce"] = Coalesce,
            ["Require"] = Require,
            ["When"] = node => Flag(node.Operands[0]) ? Maybe(node.Operands[1]) : Maybe(node.Operands[2]),
            ["Round"] = node => Math.Round(Number(node.Operands[0]), node.Places!.Value, Enum.Parse<MidpointRounding>(node.Mode!)),
            ["Guard"] = Guard,
        };
        _flags = new Dictionary<string, Func<CalculationNode, bool>>(StringComparer.Ordinal)
        {
            ["Field"] = node => ReadFlag(node.Name!),
            ["Constant"] = node => node.Flag!.Value,
            ["Equal"] = node => Compare(node, (left, right) => left == right),
            ["NotEqual"] = node => Compare(node, (left, right) => left != right),
            ["Greater"] = node => Compare(node, (left, right) => left > right),
            ["GreaterOrEqual"] = node => Compare(node, (left, right) => left >= right),
            ["Less"] = node => Compare(node, (left, right) => left < right),
            ["LessOrEqual"] = node => Compare(node, (left, right) => left <= right),
            ["And"] = node => node.Operands.All(Flag),
            ["Or"] = node => node.Operands.Any(Flag),
            ["Not"] = node => !Flag(node.Operands[0]),
            ["IsEmpty"] = node => !Maybe(node.Operands[0]).HasValue,
        };
    }

    public static ReferenceOutcome Run(CalculationArrayDefinition array, Dictionary<string, object?> record)
    {
        var evaluator = new ReferenceEvaluator(record);
        var emitted = new List<decimal>();
        try
        {
            foreach (var result in array.Results)
            {
                var value = evaluator.Number(result.Expression);
                evaluator._results[result.Name] = value;
                if (result.Emit)
                {
                    emitted.Add(value);
                }
            }
        }
        catch (ReferenceFailure failure)
        {
            return new ReferenceOutcome(failure.Code, emitted);
        }
        catch (OverflowException)
        {
            return new ReferenceOutcome("Overflow", emitted);
        }

        return new ReferenceOutcome(null, emitted);
    }

    private decimal Number(CalculationNode node)
    {
        var value = Maybe(node);
        return value.HasValue ? value.Value : throw new ReferenceFailure("EmptyOperand");
    }

    private decimal? Maybe(CalculationNode node) => _numbers[node.Op](node);

    private bool Flag(CalculationNode node) => _flags[node.Op](node);

    private decimal? Fold(CalculationNode node, Func<decimal, decimal, decimal> combine)
    {
        var result = Number(node.Operands[0]);
        for (var i = 1; i < node.Operands.Count; i++)
        {
            result = combine(result, Number(node.Operands[i]));
        }

        return result;
    }

    private decimal? Divide(CalculationNode node)
    {
        var left = Number(node.Operands[0]);
        var right = Number(node.Operands[1]);
        return right == 0m ? throw new ReferenceFailure("DivisionByZero") : left / right;
    }

    private decimal? Coalesce(CalculationNode node)
    {
        foreach (var operand in node.Operands)
        {
            var value = Maybe(operand);
            if (value.HasValue)
            {
                return value;
            }
        }

        return null;
    }

    private decimal? Require(CalculationNode node)
    {
        var value = Maybe(node.Operands[0]);
        return value.HasValue ? value : throw new ReferenceFailure("RequiredValueEmpty");
    }

    private decimal? Guard(CalculationNode node) =>
        Flag(node.Operands[0]) ? Maybe(node.Operands[1]) : throw new ReferenceFailure("GuardFailed");

    private bool Compare(CalculationNode node, Func<decimal, decimal, bool> test)
    {
        var left = Maybe(node.Operands[0]);
        var right = Maybe(node.Operands[1]);
        return left.HasValue && right.HasValue && test(left.Value, right.Value);
    }

    private decimal? ReadNumber(string name) => _record[name] switch
    {
        null => null,
        decimal exact => exact,
        int whole => whole,
        long whole => whole,
        _ => throw new InvalidOperationException($"the reference evaluator does not read {name}"),
    };

    private bool ReadFlag(string name) => _record[name] switch
    {
        bool flag => flag,
        int whole => whole == 1,
        _ => throw new InvalidOperationException($"the reference evaluator does not read {name} as a flag"),
    };
}
