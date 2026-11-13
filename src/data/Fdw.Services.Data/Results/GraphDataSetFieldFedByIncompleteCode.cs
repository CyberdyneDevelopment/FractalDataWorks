using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A field out of a Graph data set names the component that feeds it and not the output field, or the output field and not the component.
/// </summary>
/// <remarks>Refused when the graph is saved (FDW-5287): a field is fed by a component output, which takes both names, or by nothing yet. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetFieldFedByIncomplete", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetFieldFedByIncompleteCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetFieldFedByIncompleteCode"/> class.</summary>
    public GraphDataSetFieldFedByIncompleteCode()
        : base(21035, "GraphDataSetFieldFedByIncomplete",
            ResultSeverities.ByName("Error"),
            "Field '{field}' of Graph data set '{dataSet}' states only part of what feeds it: state both fedByComponent and fedByField, or neither",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
