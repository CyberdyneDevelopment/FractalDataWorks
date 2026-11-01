using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// Detaching a source from a DataSet failed.
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "SourceDetachFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceDetachFailedCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceDetachFailedCode"/> class.
    /// </summary>
    public SourceDetachFailedCode()
        : base(91100, "SourceDetachFailed", ResultSeverities.ByName("Error"),
            "Failed to detach source '{SourceName}' from DataSet '{DataSetName}': {Error}",
            isRetryable: true)
    {
    }
}
