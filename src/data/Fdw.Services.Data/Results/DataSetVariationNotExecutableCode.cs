using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A command was addressed to a data set of kind Variation through the data gateway. A variation holds no fields or sources of its own,
/// so there is nothing for the gateway to run.
/// </summary>
/// <remarks>
/// A variation is read through the data route, which resolves its chain and applies every filter in it, parent first (FDW-1032), or by
/// reading the data set it varies. Refused rather than served, because serving the base data set's rows here would drop the filters the
/// variation exists to apply. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "DataSetVariationNotExecutable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetVariationNotExecutableCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetVariationNotExecutableCode"/> class.</summary>
    public DataSetVariationNotExecutableCode()
        : base(21036, "DataSetVariationNotExecutable",
            ResultSeverities.ByName("Error"),
            "Data set '{dataSet}' is a variation of the data set {baseDataSetId}: read it through the data route, which applies its filters, or read the data set it varies",
            isRetryable: false)
    {
    }
}
