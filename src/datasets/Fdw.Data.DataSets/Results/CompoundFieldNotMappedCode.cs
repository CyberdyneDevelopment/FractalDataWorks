using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A stored field of a Compound data set (current, not calculated) that no source's field mappings name (FDW-5022): its joined query would read no column for it, and the source that holds it is never guessed. Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "CompoundFieldNotMapped", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CompoundFieldNotMappedCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFieldNotMappedCode"/> class.
    /// </summary>
    public CompoundFieldNotMappedCode()
        : base(20018, "CompoundFieldNotMapped",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' field '{field}' is in no source's field mappings; map it in the one of its sources ({sources}) that holds it, or remove the field",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
