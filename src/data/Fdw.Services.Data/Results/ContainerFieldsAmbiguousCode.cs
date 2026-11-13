using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A graph reads the fields of a container that its data store holds in more than one path, and states no path.
/// </summary>
/// <remarks>Reported when a graph is described (FDW-5523): no path is chosen for the caller (the data layer makes a container name unique within a path, not within a store). HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "ContainerFieldsAmbiguous", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerFieldsAmbiguousCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerFieldsAmbiguousCode"/> class.</summary>
    public ContainerFieldsAmbiguousCode()
        : base(21428, "ContainerFieldsAmbiguous",
            ResultSeverities.ByName("Error"),
            "Component '{component}' reads container '{container}' of data store '{dataStore}' and states no path, but the data store holds a container of that name in more than one path ({paths}): state the path",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with the graph they sent: the message names the component and what it states.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
