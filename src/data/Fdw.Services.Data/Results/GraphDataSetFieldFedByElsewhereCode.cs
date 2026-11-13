using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>A field out of a Graph data set is fed by a component other than the one that carries the fields out.</summary>
/// <remarks>Refused when the graph is saved (FDW-5287). HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetFieldFedByElsewhere", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetFieldFedByElsewhereCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetFieldFedByElsewhereCode"/> class.</summary>
    public GraphDataSetFieldFedByElsewhereCode()
        : base(21077, "GraphDataSetFieldFedByElsewhere",
            ResultSeverities.ByName("Error"),
            "Field '{field}' of Graph data set '{dataSet}' is fed by '{fedBy}', but the fields out come from '{fieldsOut}': every field out is fed by the one component that carries the fields out",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
