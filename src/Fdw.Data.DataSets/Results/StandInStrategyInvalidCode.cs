using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A stand-in request's strategy is not a registered StandInStrategies member.
/// Caller-input validation failure — HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "StandInStrategyInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StandInStrategyInvalidCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StandInStrategyInvalidCode"/> class.
    /// </summary>
    public StandInStrategyInvalidCode()
        : base(20009, "StandInStrategyInvalid",
            ResultSeverities.ByName("Error"),
            "Field '{name}' stand-in rejected: strategy '{strategy}' is not a registered StandInStrategies member",
            isRetryable: false)
    {
    }
}
