using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A data set cannot be deleted while workspace-owned variations of it exist: each one reads through to it.
/// Conflict with the data set's current state — HTTP 409 (Conflict category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "DataSetHasVariations", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetHasVariationsCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetHasVariationsCode"/> class.
    /// </summary>
    public DataSetHasVariationsCode()
        : base(41101, "DataSetHasVariations",
            ResultSeverities.ByName("Error"),
            "DataSet '{dataSet}' cannot be deleted: {count} variation(s) of it still exist",
            isRetryable: false)
    {
    }
}
