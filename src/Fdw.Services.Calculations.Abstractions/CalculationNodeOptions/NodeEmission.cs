using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.TreeResultCodes;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// What an operator needs to emit a node: the node, its operands already emitted and converted, and the compile context.
/// </summary>
public sealed class NodeEmission
{
    private static readonly MethodInfo FailMethod = typeof(CalculationRunState).GetMethod(nameof(CalculationRunState.Fail))
        ?? throw new InvalidOperationException("CalculationRunState.Fail was not found");

    private static readonly MethodInfo FailFlagMethod = typeof(CalculationRunState).GetMethod(nameof(CalculationRunState.FailFlag))
        ?? throw new InvalidOperationException("CalculationRunState.FailFlag was not found");

    private readonly Func<string, Type, Expression> _slotOf;
    private readonly Func<string, Expression> _resultOf;

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeEmission"/> class.
    /// </summary>
    /// <param name="node">The node being emitted.</param>
    /// <param name="operands">The operands, emitted and converted to the types the operator asked for.</param>
    /// <param name="expected">The type the parent asked for.</param>
    /// <param name="index">The node's index in the compiled unit, for failures.</param>
    /// <param name="state">The run state parameter of the compiled code.</param>
    /// <param name="slotOf">Gives the slot an input field was read into: field name and slot type to expression.</param>
    /// <param name="resultOf">Gives the slot of an earlier result: result name to expression.</param>
    public NodeEmission(
        CalculationNode node,
        IReadOnlyList<Expression> operands,
        Type expected,
        int index,
        ParameterExpression state,
        Func<string, Type, Expression> slotOf,
        Func<string, Expression> resultOf)
    {
        Node = node;
        Operands = operands;
        Expected = expected;
        Index = Expression.Constant(index);
        State = state;
        _slotOf = slotOf;
        _resultOf = resultOf;
    }

    /// <summary>Gets the node being emitted.</summary>
    public CalculationNode Node { get; }

    /// <summary>Gets the operands, emitted and converted to the types the operator asked for.</summary>
    public IReadOnlyList<Expression> Operands { get; }

    /// <summary>Gets the type the parent asked for.</summary>
    public Type Expected { get; }

    /// <summary>Gets the node's index in the compiled unit, as a constant expression.</summary>
    public ConstantExpression Index { get; }

    /// <summary>Gets the run state parameter of the compiled code.</summary>
    public ParameterExpression State { get; }

    /// <summary>Gets the slot an input field was read into.</summary>
    /// <param name="field">The field name.</param>
    /// <param name="type">The slot type: decimal? for a number, bool for a flag.</param>
    /// <returns>The slot.</returns>
    public Expression Slot(string field, Type type) => _slotOf(field, type);

    /// <summary>Gets the slot of an earlier result.</summary>
    /// <param name="name">The result name.</param>
    /// <returns>The slot, a decimal.</returns>
    public Expression Result(string name) => _resultOf(name);

    /// <summary>Calls a <see cref="CalculationRuntime"/> operation.</summary>
    /// <param name="method">The operation's name.</param>
    /// <param name="arguments">The arguments, including <see cref="State"/> and <see cref="Index"/> where the operation takes them.</param>
    /// <returns>The call.</returns>
    public static Expression Call(string method, params Expression[] arguments) =>
        Expression.Call(
            typeof(CalculationRuntime).GetMethod(method)
                ?? throw new InvalidOperationException($"CalculationRuntime.{method} was not found"),
            arguments);

    /// <summary>Records a failure and yields a placeholder of the given type.</summary>
    /// <param name="codeName">The name of a <see cref="CalculationTreeResultCodes"/> option.</param>
    /// <param name="type">The type the placeholder must have: decimal, decimal? or bool.</param>
    /// <returns>The expression that fails.</returns>
    public Expression Fail(string codeName, Type type)
    {
        var code = Expression.Constant(CalculationTreeResultCodes.ByName(codeName).Id);
        if (type == typeof(bool))
        {
            return Expression.Call(State, FailFlagMethod, code, Index);
        }

        var call = Expression.Call(State, FailMethod, code, Index);
        return type == typeof(decimal) ? call : Expression.Convert(call, type);
    }
}
