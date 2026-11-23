using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A data set states a source combination that is no DataSetCompositionTypes member taking several sources (FDW-6946): refused when it is
/// saved and before it is read.
/// Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "SourceCombinationNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceCombinationNotFoundCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceCombinationNotFoundCode"/> class.
    /// </summary>
    public SourceCombinationNotFoundCode()
        : base(20034, "SourceCombinationNotFound",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' states '{combination}', which is not a way sources combine; state one of: {combinations}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
