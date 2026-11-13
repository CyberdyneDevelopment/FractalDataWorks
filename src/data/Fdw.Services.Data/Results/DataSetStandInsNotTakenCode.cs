using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A route asked to read or write stand-ins or samples for a data set whose kind does not read them when it is read, and names no field
/// </summary>
/// <remarks>
/// The data-set-level reason of <c>DataSetFieldStandInsNotTaken</c> (FDW-5505): the routes that list the stand-ins or samples of a whole data set name no field, so the message
/// names only the data set. Refused by name, never answered as a change that has no effect. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "DataSetStandInsNotTaken", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataSetStandInsNotTakenCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetStandInsNotTakenCode"/> class.</summary>
    public DataSetStandInsNotTakenCode()
        : base(21417, "DataSetStandInsNotTaken",
            ResultSeverities.ByName("Error"),
            "Data set '{dataSet}' does not read stand-ins or samples for its fields, so none can be set, read, listed or cleared on it",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller which of their data sets takes no stand-ins or samples.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
