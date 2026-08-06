using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// federationStrategy is required when serviceOptionType is 'Federated' but was omitted.
/// Caller-input validation failure — HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "FederationStrategyRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FederationStrategyRequiredCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FederationStrategyRequiredCode"/> class.
    /// </summary>
    public FederationStrategyRequiredCode()
        : base(20002, "FederationStrategyRequired",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: federationStrategy is required when serviceOptionType is 'Federated'",
            isRetryable: false)
    {
    }
}
