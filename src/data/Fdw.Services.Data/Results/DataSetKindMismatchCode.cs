using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A data set's domain row names a kind (Simple, Compound or Federated) but the configuration read for it is not
/// of that kind, so what the kind needs to run is not there.
/// </summary>
/// <remarks>
/// Each kind is its own implementation with its own configuration contract (FDW-1031). Reaching this means the
/// implementation provider registered under the kind's name returned another kind's configuration: a wiring
/// fault, not a request the caller can correct. HTTP 500 by category.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "DataSetKindMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetKindMismatchCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetKindMismatchCode"/> class.</summary>
    public DataSetKindMismatchCode()
        : base(60003, "DataSetKindMismatch",
            ResultSeverities.ByName("Error"),
            "Data set '{dataSet}' is a {kind} data set, but its configuration is not of that kind",
            isRetryable: false)
    {
    }
}
