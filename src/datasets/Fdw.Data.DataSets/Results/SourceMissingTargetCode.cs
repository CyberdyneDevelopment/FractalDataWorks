using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A DataSet source names neither a container nor a source DataSet as its target.
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "SourceMissingTarget", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceMissingTargetCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceMissingTargetCode"/> class.
    /// </summary>
    public SourceMissingTargetCode()
        : base(21100, "SourceMissingTarget", ResultSeverities.ByName("Error"),
            "Source '{SourceName}' on DataSet '{DataSetName}' names neither a container nor a source DataSet as its target",
            isRetryable: false)
    {
    }
}
