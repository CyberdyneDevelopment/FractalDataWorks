using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A stored field of a Compound data set that two sources' field mappings both name (FDW-5022): which one it is read from is never picked. Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "CompoundFieldMappedTwice", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CompoundFieldMappedTwiceCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFieldMappedTwiceCode"/> class.
    /// </summary>
    public CompoundFieldMappedTwiceCode()
        : base(20019, "CompoundFieldMappedTwice",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' field '{field}' is mapped by both source '{firstSource}' and source '{secondSource}'; map it in only the one source that holds it",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
