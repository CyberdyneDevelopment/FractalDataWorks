using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A gate as a decision (FDW-6134): combines the values of the lines that feed it by its logic and fires <c>true</c> or <c>false</c>. A line whose output did not fire, or whose
/// decision was skipped, is false; a gate is never skipped.
/// </summary>
public sealed class FlowGateStep : IFlowSignalReader
{
    private readonly IGateLogic _logic;
    private readonly ILogger _logger;
    private IReadOnlyList<FlowSignal> _signals = [];

    /// <summary>Initializes a new instance of the <see cref="FlowGateStep"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="logic">The logic that judges the lines.</param>
    /// <param name="logger">The logger.</param>
    public FlowGateStep(string name, IGateLogic logic, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        _logic = logic;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs { get; } = [ConditionComponentType.True, ConditionComponentType.False];

    /// <inheritdoc />
    public IGenericResult ReadSignals(IReadOnlyList<FlowSignal> signals)
    {
        if (signals.Count < _logic.MinimumInputs || (_logic.MaximumInputs is { } most && signals.Count > most))
        {
            return GenericResult.Failure(FlowLog.GateInputCountInvalid(_logger, Name, signals.Count, _logic.Name));
        }

        _signals = signals;
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        var holds = _logic.Judge([.. _signals.Select(signal => FlowFired.Has(context.Variables, signal))]);
        FlowFired.Set(context.Variables, Name, ConditionComponentType.True, holds);
        FlowFired.Set(context.Variables, Name, ConditionComponentType.False, !holds);
        return Task.FromResult(GenericResult.Success());
    }
}
