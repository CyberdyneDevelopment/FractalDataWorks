using System.Collections.Generic;
using Fdw.Services.Calculations.Abstractions.Lineage;

namespace Fdw.Services.Calculations.Lineage;

/// <summary>
/// Accumulates a single calculation execution's per-step derivation in execution order.
/// </summary>
/// <remarks>
/// One instance belongs to one execution. It is deliberately not registered in DI: the executor is
/// a singleton and holds no trace state, so a shared recorder would interleave the steps of
/// concurrent calculations into a derivation that describes neither of them.
/// </remarks>
public sealed class CalculationTraceRecorder : ICalculationTraceRecorder
{
    private readonly List<CalculationStepTrace> _steps = [];

    /// <inheritdoc />
    public IReadOnlyList<CalculationStepTrace> Steps => _steps;

    /// <inheritdoc />
    public void Record(CalculationStepTrace step) => _steps.Add(step);
}
