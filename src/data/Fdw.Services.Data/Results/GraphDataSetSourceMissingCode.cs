using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A component of a Graph data set reads a data set or data store that does not exist.
/// </summary>
/// <remarks>Reported by the check before save (FDW-5290), path <c>components/{component}</c>. An intended source of an abstract data set is not a component and is never reported here (see notBound in the check's answer). HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetSourceMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetSourceMissingCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetSourceMissingCode"/> class.</summary>
    public GraphDataSetSourceMissingCode()
        : base(21405, "GraphDataSetSourceMissing",
            ResultSeverities.ByName("Error"),
            "Component '{component}' of Graph data set '{dataSet}' reads {sourceKind} '{source}', which does not exist",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
