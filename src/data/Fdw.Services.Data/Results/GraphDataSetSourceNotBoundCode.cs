using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A bound Graph data set reads an abstract Graph data set that nothing feeds yet.
/// </summary>
/// <remarks>Reported by the check before save (FDW-5290), path <c>components/{component}</c>. An abstract data set reading another abstract one is accepted; only a graph that is bound blocks, because it would be saved reading fields no component feeds. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetSourceNotBound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetSourceNotBoundCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetSourceNotBoundCode"/> class.</summary>
    public GraphDataSetSourceNotBoundCode()
        : base(21406, "GraphDataSetSourceNotBound",
            ResultSeverities.ByName("Error"),
            "Component '{component}' of Graph data set '{dataSet}' reads Graph data set '{source}', which is abstract: nothing feeds its fields yet, so a bound graph cannot read it",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
