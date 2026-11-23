using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A Graph data set's graph holds a decision or a container component (Condition, Gate, Switch or Container).
/// </summary>
/// <remarks>Refused when the graph is saved, never stored and left to fail at read (FDW-6134). HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetControlRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetControlRefusedCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetControlRefusedCode"/> class.</summary>
    public GraphDataSetControlRefusedCode()
        : base(21040, "GraphDataSetControlRefused",
            ResultSeverities.ByName("Error"),
            "Component '{component}' of Graph data set '{dataSet}' is of kind '{kind}', which steers which components run: a Graph data set's graph has none",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
