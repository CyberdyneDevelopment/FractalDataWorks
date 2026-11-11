using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A Compound or Federated data set's join names a join type that joins nothing (FDW-5087, FDW-5099): an option that neither joins on a
/// condition nor keeps every row of both sides ("None"). Refused when the data set is saved and before it is read, never run as another join.
/// Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "JoinTypeNotSupported", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JoinTypeNotSupportedCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JoinTypeNotSupportedCode"/> class.
    /// </summary>
    public JoinTypeNotSupportedCode()
        : base(20028, "JoinTypeNotSupported",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' join {ordinal} has join type '{joinType}', which joins nothing; use one of: {joinTypes}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
