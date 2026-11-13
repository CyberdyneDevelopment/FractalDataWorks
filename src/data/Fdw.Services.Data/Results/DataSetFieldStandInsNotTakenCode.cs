using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A route asked to set, read, clear or preview a stand-in or a sample on a field of a data set whose kind does not read them when it is read
/// </summary>
/// <remarks>
/// A route asked to set, read, clear or preview a stand-in or a sample on a field of a data set whose kind does not read them when it is read (FDW-5505). Refused by name, never answered as a change that has no effect. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "DataSetFieldStandInsNotTaken", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetFieldStandInsNotTakenCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetFieldStandInsNotTakenCode"/> class.</summary>
    public DataSetFieldStandInsNotTakenCode()
        : base(21416, "DataSetFieldStandInsNotTaken",
            ResultSeverities.ByName("Error"),
            "Data set '{dataSet}' does not read stand-ins or samples for its fields, so none can be set, read or cleared on field '{field}'",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller which of their data sets takes no stand-ins or samples.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
