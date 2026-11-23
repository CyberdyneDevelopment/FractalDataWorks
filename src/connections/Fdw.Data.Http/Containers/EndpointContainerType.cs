using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Http.Containers;

/// <summary>
/// Container type for REST API endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ContainerTypes), "Endpoint")]
public sealed class EndpointContainerType : ContainerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointContainerType"/> class.
    /// </summary>
    public EndpointContainerType()
        : base(
            name: "Endpoint",
            displayName: "REST Endpoint",
            description: "REST API endpoint container with HTTP method support",
            supportsSchemaDiscovery: true,
            supportedOperations: ["Query"])
    {
    }
}
