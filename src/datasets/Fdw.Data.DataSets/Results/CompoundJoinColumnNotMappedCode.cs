using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A Compound data set's join names a field its source's field mappings do not hold (FDW-5022): the join would compare a column that is not there, and the logical name is never sent in its place. Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "CompoundJoinColumnNotMapped", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CompoundJoinColumnNotMappedCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundJoinColumnNotMappedCode"/> class.
    /// </summary>
    public CompoundJoinColumnNotMappedCode()
        : base(20020, "CompoundJoinColumnNotMapped",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' join from '{leftSource}' to '{rightSource}' names field '{field}', which source '{source}' does not map; map it in that source",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
