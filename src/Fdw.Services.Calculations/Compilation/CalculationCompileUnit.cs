using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.TreeResultCodes;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Compilation;

/// <summary>
/// The working state of compiling one calculation array: the parameters of the delegate, the slots inputs and results are
/// kept in, and the labels failures are named by.
/// </summary>
/// <remarks>
/// Why slots: an input field read by many results is read from the record once, in a prologue, into a typed local; each
/// result is a local too, so a Ref is a variable read and an intermediate shared by thirty results is computed once.
/// Every field a tree names is therefore read before any result runs, and must be present in the record.
/// </remarks>
internal sealed class CalculationCompileUnit
{
    private static readonly Type AnyNumber = typeof(decimal?);

    private static readonly MethodInfo FailWithMethod = typeof(CalculationRunState).GetMethod(nameof(CalculationRunState.FailWith))
        ?? throw new InvalidOperationException("CalculationRunState.FailWith was not found");

    private readonly ParameterExpression _record = Expression.Parameter(typeof(IReadOnlyDictionary<string, object?>), "record");
    private readonly ParameterExpression _state = Expression.Parameter(typeof(CalculationRunState), "state");
    private readonly ParameterExpression _results = Expression.Parameter(typeof(decimal[]), "results");
    private readonly List<string> _labels = new();
    private readonly List<ParameterExpression> _variables = new();
    private readonly List<Expression> _prologue = new();
    private readonly Dictionary<string, ParameterExpression> _fieldSlots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ParameterExpression> _resultSlots = new(StringComparer.Ordinal);

    public List<string> Labels => _labels;

    // Emits the statement that computes one result into its slot, and into the results array when it is written out. The slot
    // is entered in the result table only after the tree is emitted, so a result cannot reach itself.
    public ConditionalExpression Statement(CalculationResultDefinition result, int resultIndex, int emittedIndex)
    {
        var rootIndex = _labels.Count;
        var slot = Expression.Variable(typeof(decimal), result.Name);
        var steps = new List<Expression>
        {
            Expression.Assign(Expression.Property(_state, nameof(CalculationRunState.Current)), Expression.Constant(resultIndex)),
            Expression.Assign(slot, CalculationCoercion.Coerce(Emit(result.Expression, typeof(decimal)), typeof(decimal), _state, rootIndex)),
        };
        _variables.Add(slot);
        _resultSlots[result.Name] = slot;
        if (emittedIndex >= 0)
        {
            steps.Add(Expression.Assign(Expression.ArrayAccess(_results, Expression.Constant(emittedIndex)), slot));
        }

        return Expression.IfThen(
            Expression.Not(Expression.Property(_state, nameof(CalculationRunState.Failed))),
            Expression.Block(typeof(void), steps));
    }

    // Wraps the field reads and the result statements in one body. An arithmetic overflow is caught here and recorded, so a
    // number that does not fit a decimal fails the record the same way a division by zero does.
    public Action<IReadOnlyDictionary<string, object?>, CalculationRunState, decimal[]> Compile(List<Expression> statements) =>
        Expression.Lambda<Action<IReadOnlyDictionary<string, object?>, CalculationRunState, decimal[]>>(
            Expression.Block(
                typeof(void),
                _variables,
                Expression.TryCatch(Expression.Block(typeof(void), _prologue.Concat(statements)), OverflowHandler())),
            _record,
            _state,
            _results).Compile();

    private CatchBlock OverflowHandler() =>
        Expression.Catch(
            typeof(OverflowException),
            Expression.Block(
                typeof(void),
                Expression.Call(
                    _state,
                    FailWithMethod,
                    Expression.Constant(CalculationTreeResultCodes.ByName("Overflow").Id),
                    Expression.Constant(-1),
                    Expression.Constant("the arithmetic result does not fit a decimal"))));

    private Expression Emit(CalculationNode node, Type expected)
    {
        var index = _labels.Count;
        _labels.Add(node.Describe());
        var option = CalculationNodeTypes.ByName(node.Op);
        var operands = new List<Expression>(node.Operands.Count);
        for (var i = 0; i < node.Operands.Count; i++)
        {
            var required = option.OperandType(i);
            var child = Emit(node.Operands[i], required is null ? AnyNumber : required);
            operands.Add(required is null ? child : CalculationCoercion.Coerce(child, required, _state, index));
        }

        return option.Emit(new NodeEmission(node, operands, expected, index, _state, (field, type) => FieldSlot(field, type, index), ResultSlot));
    }

    private ParameterExpression FieldSlot(string field, Type type, int node)
    {
        var key = (type == typeof(bool) ? "flag:" : "number:") + field;
        if (_fieldSlots.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var slot = Expression.Variable(type, field);
        _variables.Add(slot);
        _fieldSlots[key] = slot;
        _prologue.Add(Expression.Assign(slot, ReadCall(field, type, node)));
        return slot;
    }

    private MethodCallExpression ReadCall(string field, Type type, int node)
    {
        var reader = type == typeof(bool) ? nameof(CalculationRuntime.ReadFlag) : nameof(CalculationRuntime.ReadNumber);
        return Expression.Call(
            typeof(CalculationRuntime).GetMethod(reader) ?? throw new InvalidOperationException($"CalculationRuntime.{reader} was not found"),
            _record,
            Expression.Constant(field),
            _state,
            Expression.Constant(node));
    }

    private ParameterExpression ResultSlot(string name) =>
        _resultSlots.TryGetValue(name, out var slot)
            ? slot
            : throw new InvalidOperationException($"Ref '{name}' reached the compiler before the result it names; validation refuses it");
}
