using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A source of a data set names no data store (FDW-6806): the data set's kind is worked out from its sources' data stores, so the store is never
/// assumed. Refused when the data set is saved. Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "SourceDataStoreMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceDataStoreMissingCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceDataStoreMissingCode"/> class.
    /// </summary>
    public SourceDataStoreMissingCode()
        : base(20038, "SourceDataStoreMissing",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' source '{source}' names no data store; a data set's kind is worked out from its sources' data stores, so each source names one",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
