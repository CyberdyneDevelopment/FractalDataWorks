using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A graph reads the fields of a container in a path of its data store that does not hold it.
/// </summary>
/// <remarks>Reported when a graph is described (FDW-5523): the path is the one the component states, and the data store holds the container only in other paths. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "ContainerFieldsInOtherPath", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerFieldsInOtherPathCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerFieldsInOtherPathCode"/> class.</summary>
    public ContainerFieldsInOtherPathCode()
        : base(21427, "ContainerFieldsInOtherPath",
            ResultSeverities.ByName("Error"),
            "Component '{component}' reads container '{container}' of data store '{dataStore}' in path '{path}', but the data store holds that container only in: {foundPath}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with the graph they sent: the message names the component and what it states.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
