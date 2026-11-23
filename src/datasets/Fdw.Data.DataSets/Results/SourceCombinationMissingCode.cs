using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A Compound or Federated data set with more than one source states how its sources combine, a join or a union (FDW-6946). There is no default and
/// nothing is worked out from whether it has joins: a data set that states neither is refused when it is saved and before it is read. Caller-input
/// validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "SourceCombinationMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceCombinationMissingCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceCombinationMissingCode"/> class.
    /// </summary>
    public SourceCombinationMissingCode()
        : base(20033, "SourceCombinationMissing",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' has {sourceCount} sources and states neither a join nor a union; state how they combine: {combinations}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
