using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A data set's own filter condition uses an operator that takes no value (IsNull, IsNotNull) and states one (FDW-4997). Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "DataSetFilterValueNotAllowed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetFilterValueNotAllowedCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetFilterValueNotAllowedCode"/> class.
    /// </summary>
    public DataSetFilterValueNotAllowedCode()
        : base(20026, "DataSetFilterValueNotAllowed",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' filter {ordinal} on field '{field}' uses operator '{operator}', which takes no value, and states '{value}'",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
