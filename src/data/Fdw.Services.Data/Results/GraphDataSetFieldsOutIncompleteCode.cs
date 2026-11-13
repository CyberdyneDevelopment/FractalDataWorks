using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>A Graph data set names the component that carries its fields out and not the output node, or the output node and not the component.</summary>
/// <remarks>Refused when the graph is saved (FDW-5287). HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetFieldsOutIncomplete", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetFieldsOutIncompleteCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetFieldsOutIncompleteCode"/> class.</summary>
    public GraphDataSetFieldsOutIncompleteCode()
        : base(21076, "GraphDataSetFieldsOutIncomplete",
            ResultSeverities.ByName("Error"),
            "Graph data set '{dataSet}' states only part of where its fields out come from: state both fieldsOutComponent and fieldsOutOutput, or neither",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
