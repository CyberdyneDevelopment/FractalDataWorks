using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A Graph data set's graph holds an action component (Execute SQL, Execute pipeline, Notify, Set variable or Read data options), which has an effect and moves no rows.
/// </summary>
/// <remarks>Refused when the graph is saved, never stored and left to fail at read (FDW-5287). HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetActionRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetActionRefusedCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetActionRefusedCode"/> class.</summary>
    public GraphDataSetActionRefusedCode()
        : base(21039, "GraphDataSetActionRefused",
            ResultSeverities.ByName("Error"),
            "Component '{component}' of Graph data set '{dataSet}' is of kind '{kind}', an action: a Graph data set is read for its rows, so its graph has no action",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
