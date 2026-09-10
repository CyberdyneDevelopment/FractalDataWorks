using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// The requested/merged Implementation is missing or not a registered DataSetTypes strategy
/// (Simple/Compound/Federated). Caller-input validation failure — HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "ImplementationInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ImplementationInvalidCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImplementationInvalidCode"/> class.
    /// </summary>
    public ImplementationInvalidCode()
        : base(20001, "ImplementationInvalid",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: Implementation '{implementation}' is not a registered DataSetTypes strategy (Simple/Compound/Federated)",
            isRetryable: false)
    {
    }
}
