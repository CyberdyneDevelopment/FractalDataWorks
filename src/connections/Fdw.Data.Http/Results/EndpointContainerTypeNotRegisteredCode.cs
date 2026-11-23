using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Http.Results;

/// <summary>
/// The container type an endpoint container names, Endpoint, is not registered in ContainerTypes, so the container cannot be built
/// (FDW-6309): the lookup answered the NotFound sentinel, and a container is never built with it.
/// </summary>
[TypeOption(typeof(DataHttpResultCodes), "EndpointContainerTypeNotRegistered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EndpointContainerTypeNotRegisteredCode : DataHttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointContainerTypeNotRegisteredCode"/> class.
    /// </summary>
    public EndpointContainerTypeNotRegisteredCode()
        : base(60001, "EndpointContainerTypeNotRegistered",
            ResultSeverities.ByName("Error"),
            "Container type 'Endpoint' is not registered in ContainerTypes, so endpoint container '{ContainerName}' cannot be built",
            isRetryable: false)
    {
    }
}
