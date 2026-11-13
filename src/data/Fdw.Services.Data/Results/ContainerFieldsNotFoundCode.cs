using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A graph reads the fields of a container its data store does not hold.
/// </summary>
/// <remarks>Reported when a graph is described (FDW-5523): judged from the data store's stored containers, with no connection to the store. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "ContainerFieldsNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerFieldsNotFoundCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerFieldsNotFoundCode"/> class.</summary>
    public ContainerFieldsNotFoundCode()
        : base(21426, "ContainerFieldsNotFound",
            ResultSeverities.ByName("Error"),
            "Component '{component}' reads container '{container}' of data store '{dataStore}', which the data store does not hold",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with the graph they sent: the message names the component and what it states.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
