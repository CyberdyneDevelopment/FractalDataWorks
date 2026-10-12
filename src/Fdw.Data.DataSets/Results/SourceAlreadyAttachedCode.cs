using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A source with the given name is already attached to the target DataSet.
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "SourceAlreadyAttached", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceAlreadyAttachedCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceAlreadyAttachedCode"/> class.
    /// </summary>
    public SourceAlreadyAttachedCode()
        : base(41100, "SourceAlreadyAttached", ResultSeverities.ByName("Error"),
            "Source '{SourceName}' is already attached to DataSet '{DataSetName}'",
            isRetryable: false)
    {
    }
}
