using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A Compound or Federated data set's join states no join type (FDW-5099). There is no default join type: refused when the data set is saved
/// and before it is read, never stored or run as Inner. Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "JoinTypeMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JoinTypeMissingCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JoinTypeMissingCode"/> class.
    /// </summary>
    public JoinTypeMissingCode()
        : base(20030, "JoinTypeMissing",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' join {ordinal} states no join type; state one of: {joinTypes}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
