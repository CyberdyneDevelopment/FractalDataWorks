using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A field out is fed by a component that is not in the graph.
/// </summary>
/// <remarks>Reported by the check before save (FDW-5290), path <c>fieldsOut/{field}</c>: the field states the component and the component output field that feed it, and the component is not in the graph. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetFieldOutFeedMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetFieldOutFeedMissingCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetFieldOutFeedMissingCode"/> class.</summary>
    public GraphDataSetFieldOutFeedMissingCode()
        : base(21401, "GraphDataSetFieldOutFeedMissing",
            ResultSeverities.ByName("Error"),
            "Field out '{field}' of Graph data set '{dataSet}' is fed by component '{component}', which is not in the graph",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
