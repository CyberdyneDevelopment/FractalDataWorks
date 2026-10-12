using System.Collections.Generic;

namespace Fdw.Services.Calculations.Tests.CalculationTrees;

/// <summary>
/// What the reference evaluator produced for one record: the first failure's code name, or null, and the emitted results.
/// </summary>
internal sealed class ReferenceOutcome
{
    public ReferenceOutcome(string? failure, List<decimal> emitted)
    {
        Failure = failure;
        Emitted = emitted;
    }

    public string? Failure { get; }

    public List<decimal> Emitted { get; }
}
