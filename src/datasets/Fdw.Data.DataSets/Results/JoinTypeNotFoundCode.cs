using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A Compound or Federated data set's join names a join type that is not one (FDW-5099): no JoinTypes option carries the name. Refused when
/// the data set is saved and before it is read, never written into a query or run as another join. Caller-input validation failure:
/// HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "JoinTypeNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JoinTypeNotFoundCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JoinTypeNotFoundCode"/> class.
    /// </summary>
    public JoinTypeNotFoundCode()
        : base(20029, "JoinTypeNotFound",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' join {ordinal} has join type '{joinType}', which is not a join type; use one of: {joinTypes}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
