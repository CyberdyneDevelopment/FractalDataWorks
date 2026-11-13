using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A component of a Graph data set reads a container its data store holds in more than one path, and states no path.
/// </summary>
/// <remarks>
/// Reported by the check before save (FDW-5408), path <c>components/{component}</c>. The data layer makes a container name unique within a path, not within a data store, so
/// one name can be held in two paths; the check does not pick one, it asks the component to state its path. A component that states a path one of them is in is not reported. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetContainerAmbiguous", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetContainerAmbiguousCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetContainerAmbiguousCode"/> class.</summary>
    public GraphDataSetContainerAmbiguousCode()
        : base(21422, "GraphDataSetContainerAmbiguous",
            ResultSeverities.ByName("Error"),
            "Component '{component}' of Graph data set '{dataSet}' reads container '{container}' of data store '{dataStore}' and states no path, but the data store holds a container of that name in more than one path ({paths}): state the path",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
