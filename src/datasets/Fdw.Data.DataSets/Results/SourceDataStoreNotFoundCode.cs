using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A source of a data set names a data store that does not exist (FDW-6806): the kind is worked out from the stores the sources name, so a store that
/// is not there is refused, never treated as a store of its own. Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "SourceDataStoreNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceDataStoreNotFoundCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceDataStoreNotFoundCode"/> class.
    /// </summary>
    public SourceDataStoreNotFoundCode()
        : base(20039, "SourceDataStoreNotFound",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' source '{source}' names data store '{dataStore}', which is not a data store",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
