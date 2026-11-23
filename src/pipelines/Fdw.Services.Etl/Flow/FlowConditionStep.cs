using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>A condition as a decision (FDW-6134): judges its condition over the variables and fires <c>true</c> or <c>false</c>.</summary>
public sealed class FlowConditionStep : IFlowDecision
{
    private readonly Func<FlowVariables, bool> _condition;

    /// <summary>Initializes a new instance of the <see cref="FlowConditionStep"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="condition">The condition, judged over the variables in scope when the step runs.</param>
    public FlowConditionStep(string name, Func<FlowVariables, bool> condition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        _condition = condition;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs { get; } = [ConditionComponentType.True, ConditionComponentType.False];

    /// <inheritdoc />
    public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        var holds = _condition(context.Variables);
        FlowFired.Set(context.Variables, Name, ConditionComponentType.True, holds);
        FlowFired.Set(context.Variables, Name, ConditionComponentType.False, !holds);
        return Task.FromResult(GenericResult.Success());
    }
}
