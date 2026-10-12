using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// The supplied federationStrategy is not a registered FederationStrategies member.
/// Caller-input validation failure — HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "FederationStrategyInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FederationStrategyInvalidCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FederationStrategyInvalidCode"/> class.
    /// </summary>
    public FederationStrategyInvalidCode()
        : base(20003, "FederationStrategyInvalid",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: federationStrategy '{federationStrategy}' is not a registered FederationStrategies member",
            isRetryable: false)
    {
    }
}
