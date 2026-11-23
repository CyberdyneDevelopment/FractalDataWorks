using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A data set states a join with no joins, or a union with joins (FDW-6946): a data set states a join or a union, never both. Refused when it is
/// saved and before it is read. Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "SourceCombinationJoinsMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceCombinationJoinsMismatchCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceCombinationJoinsMismatchCode"/> class.
    /// </summary>
    public SourceCombinationJoinsMismatchCode()
        : base(20035, "SourceCombinationJoinsMismatch",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' states {combination} and has {joinCount} join(s); a join needs at least one join and a union has none",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
