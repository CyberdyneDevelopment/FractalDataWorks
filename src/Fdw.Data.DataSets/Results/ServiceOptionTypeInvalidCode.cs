using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// The requested/merged ServiceOptionType is missing or not a registered DataSetTypes strategy
/// (Simple/Compound/Federated). Caller-input validation failure — HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "ServiceOptionTypeInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ServiceOptionTypeInvalidCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceOptionTypeInvalidCode"/> class.
    /// </summary>
    public ServiceOptionTypeInvalidCode()
        : base(20001, "ServiceOptionTypeInvalid",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: ServiceOptionType '{serviceOptionType}' is not a registered DataSetTypes strategy (Simple/Compound/Federated)",
            isRetryable: false)
    {
    }
}
