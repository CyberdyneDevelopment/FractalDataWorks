using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A Graph data set's graph holds a loop component (Do while, For each or For loop).
/// </summary>
/// <remarks>Refused when the graph is saved, never stored and left to fail at read (FDW-5287). HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetLoopRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetLoopRefusedCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetLoopRefusedCode"/> class.</summary>
    public GraphDataSetLoopRefusedCode()
        : base(21031, "GraphDataSetLoopRefused",
            ResultSeverities.ByName("Error"),
            "Component '{component}' of Graph data set '{dataSet}' is of kind '{kind}', a loop: a Graph data set's graph has no loop",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
