using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// Something that cites a data set by its durable Id (a variation's base data set) names one that no current data set has (FDW-6129). A data
/// set's row is versioned on write, so its Id repeats across versions and no foreign key can hold the citation; the write that makes it checks the
/// data set exists and is refused otherwise. Not-found classification — caller-input failure.
/// </summary>
/// <remarks>The message is the caller's own input refused, so it is shown to them.</remarks>
[TypeOption(typeof(DataSetsResultCodes), "CitedDataSetNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CitedDataSetNotFoundCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CitedDataSetNotFoundCode"/> class.
    /// </summary>
    public CitedDataSetNotFoundCode()
        : base(31102, "CitedDataSetNotFound", ResultSeverities.ByName("Error"),
            "'{name}' cites data set {dataSetId} as its {citation}, and no current data set has that Id",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
