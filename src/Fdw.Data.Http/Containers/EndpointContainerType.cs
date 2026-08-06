using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Http.Containers;

/// <summary>
/// Container type for REST API endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ContainerTypes), "Endpoint", RestrictToCurrentCompilation = true)]
public sealed class EndpointContainerType : ContainerTypeBase
{
    /// <summary>
    /// Singleton instance of EndpointContainerType.
    /// </summary>
    public static readonly EndpointContainerType Instance = new();

    private EndpointContainerType()
        : base(
            id: 10,
            name: "Endpoint",
            displayName: "REST Endpoint",
            description: "REST API endpoint container with HTTP method support",
            supportsSchemaDiscovery: true)
    {
    }
}
