using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// The container named as a DataSet source's target was not found.
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "SourceContainerNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceContainerNotFoundCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceContainerNotFoundCode"/> class.
    /// </summary>
    public SourceContainerNotFoundCode()
        : base(31101, "SourceContainerNotFound", ResultSeverities.ByName("Error"),
            "Source container '{ContainerName}' referenced by DataSet '{DataSetName}' was not found",
            isRetryable: false)
    {
    }
}
