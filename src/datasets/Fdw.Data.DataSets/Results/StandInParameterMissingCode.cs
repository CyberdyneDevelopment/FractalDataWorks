using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A stand-in strategy's request is missing one of the parameters that strategy requires.
/// Caller-input validation failure — HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "StandInParameterMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StandInParameterMissingCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StandInParameterMissingCode"/> class.
    /// </summary>
    public StandInParameterMissingCode()
        : base(20008, "StandInParameterMissing",
            ResultSeverities.ByName("Error"),
            "Field '{name}' stand-in rejected: strategy requires '{field}', which was not supplied",
            isRetryable: false)
    {
    }
}
