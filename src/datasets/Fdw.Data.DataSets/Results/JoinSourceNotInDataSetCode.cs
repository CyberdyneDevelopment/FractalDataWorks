using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A join of a Compound or Federated data set names a source that is not one of the data set's sources (FDW-5022): it is refused when the
/// data set is saved and when it is read, never joined to nothing. Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "JoinSourceNotInDataSet", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class JoinSourceNotInDataSetCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JoinSourceNotInDataSetCode"/> class.
    /// </summary>
    public JoinSourceNotInDataSetCode()
        : base(20021, "JoinSourceNotInDataSet",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' join {ordinal} names source '{source}', which is not one of its sources ({sources}); name one of them",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
