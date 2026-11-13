using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A route or an update named an intended source that the Graph data set does not have.
/// </summary>
/// <remarks>Intended sources are addressed by their stable id (FDW-5452); an id that matches none is refused, never made into a new intended source. HTTP 404.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetIntendedSourceNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetIntendedSourceNotFoundCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetIntendedSourceNotFoundCode"/> class.</summary>
    public GraphDataSetIntendedSourceNotFoundCode()
        : base(31432, "GraphDataSetIntendedSourceNotFound",
            ResultSeverities.ByName("Error"),
            "Intended source '{source}' is not an intended source of Graph data set '{dataSet}'",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller which intended source the Graph data set does not have.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
