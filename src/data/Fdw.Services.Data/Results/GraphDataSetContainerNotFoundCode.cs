using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A component of a Graph data set reads a container its data store does not hold.
/// </summary>
/// <remarks>Reported by the check before save (FDW-5408), path <c>components/{component}</c>. Judged from the data store's stored containers, with no connection to the store; the data store itself is judged apart (a data store that does not exist is GraphDataSetSourceMissing). HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetContainerNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetContainerNotFoundCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetContainerNotFoundCode"/> class.</summary>
    public GraphDataSetContainerNotFoundCode()
        : base(21420, "GraphDataSetContainerNotFound",
            ResultSeverities.ByName("Error"),
            "Component '{component}' of Graph data set '{dataSet}' reads container '{container}' of data store '{dataStore}', which the data store does not hold",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
