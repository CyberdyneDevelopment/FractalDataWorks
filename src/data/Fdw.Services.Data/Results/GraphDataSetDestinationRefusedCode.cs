using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A Graph data set's graph holds a destination component, which writes rows; a Graph data set stores nothing.
/// </summary>
/// <remarks>Refused when the graph is saved, never stored and left to fail at read (FDW-5287). HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetDestinationRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetDestinationRefusedCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetDestinationRefusedCode"/> class.</summary>
    public GraphDataSetDestinationRefusedCode()
        : base(21038, "GraphDataSetDestinationRefused",
            ResultSeverities.ByName("Error"),
            "Component '{component}' of Graph data set '{dataSet}' is of kind '{kind}', a destination: a Graph data set is read and stores nothing, so its graph has no destination",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
