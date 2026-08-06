using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// federationStrategy was supplied for a non-Federated dataset. Caller-input validation
/// failure — HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "FederationStrategyNotAllowed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FederationStrategyNotAllowedCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FederationStrategyNotAllowedCode"/> class.
    /// </summary>
    public FederationStrategyNotAllowedCode()
        : base(20004, "FederationStrategyNotAllowed",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: federationStrategy is not allowed when serviceOptionType is '{serviceOptionType}'",
            isRetryable: false)
    {
    }
}
