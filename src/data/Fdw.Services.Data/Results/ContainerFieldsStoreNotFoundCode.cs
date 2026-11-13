using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A graph reads the fields of a container in a data store that does not exist.
/// </summary>
/// <remarks>Reported when a graph is described (FDW-5523): the fields of a container source are read from the data store's stored containers, and the data store is not there. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "ContainerFieldsStoreNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerFieldsStoreNotFoundCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerFieldsStoreNotFoundCode"/> class.</summary>
    public ContainerFieldsStoreNotFoundCode()
        : base(21425, "ContainerFieldsStoreNotFound",
            ResultSeverities.ByName("Error"),
            "Component '{component}' reads container '{container}' of data store '{dataStore}', which does not exist",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with the graph they sent: the message names the component and what it states.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
