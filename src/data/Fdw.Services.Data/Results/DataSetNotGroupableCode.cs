using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A query asked a data set to group and aggregate, and this data set cannot: its kind builds its result from
/// several reads (Compound, Federated), or it already defines aggregates of its own.
/// </summary>
/// <remarks>
/// Refused rather than served, because serving it would return the rows ungrouped. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "DataSetNotGroupable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetNotGroupableCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetNotGroupableCode"/> class.</summary>
    public DataSetNotGroupableCode()
        : base(20005, "DataSetNotGroupable",
            ResultSeverities.ByName("Error"),
            "Data set '{dataSet}' cannot be grouped: {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the data set cannot be read this way: the message names their own data set and the kind's limit.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
