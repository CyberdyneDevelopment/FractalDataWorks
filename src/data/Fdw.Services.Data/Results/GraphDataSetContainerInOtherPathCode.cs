using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A component of a Graph data set states a path its container is not in.
/// </summary>
/// <remarks>Reported by the check before save (FDW-5408), path <c>components/{component}</c>, when the component states a path and the data store holds the container in another. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetContainerInOtherPath", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetContainerInOtherPathCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetContainerInOtherPathCode"/> class.</summary>
    public GraphDataSetContainerInOtherPathCode()
        : base(21421, "GraphDataSetContainerInOtherPath",
            ResultSeverities.ByName("Error"),
            "Component '{component}' of Graph data set '{dataSet}' reads container '{container}' of data store '{dataStore}' in path '{path}', but the data store holds that container only in: {foundPath}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
