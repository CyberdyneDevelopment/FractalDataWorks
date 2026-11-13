using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A route asked for the version history of a field of a data set whose kind keeps its fields apart from the shared field table and keeps no history of them
/// </summary>
/// <remarks>
/// A route asked for the version history of a field of a data set whose kind keeps its fields apart from the shared field table and keeps no history of them (FDW-5505). Refused by name, never answered as an empty history. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "DataSetFieldHistoryNotKept", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetFieldHistoryNotKeptCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetFieldHistoryNotKeptCode"/> class.</summary>
    public DataSetFieldHistoryNotKeptCode()
        : base(21415, "DataSetFieldHistoryNotKept",
            ResultSeverities.ByName("Error"),
            "Data set '{dataSet}' keeps no version history for its fields, so there is no history of field '{field}' to read",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller which of their data sets keeps no field history.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
