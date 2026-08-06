using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fdw.Collections.Attributes;
using Fdw.Services.Data.Abstractions;
using Fdw.SignalR;

namespace Fdw.Services.Data.SignalR;

/// <summary>
/// Registers the schema-discovery hub against the <see cref="RealTimeHubs"/> collection.
/// </summary>
/// <remarks>
/// Declares the route and broadcaster wiring for <see cref="SchemaDiscoveryHub"/>. The notifier is
/// registered as a singleton, preserving the pre-existing lifetime. The hub authorizes via its own
/// <c>[Authorize]</c> attribute, so no mapping-level policy is declared here.
/// </remarks>
[TypeOption(typeof(RealTimeHubs), "SchemaDiscovery")]
public sealed class SchemaDiscoveryHubOption : RealTimeHubOptionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaDiscoveryHubOption"/> class.
    /// </summary>
    public SchemaDiscoveryHubOption()
        : base(3, "SchemaDiscovery", "/hubs/schema-discovery", typeof(SchemaDiscoveryHub), authorizationPolicy: null)
    {
    }

    /// <inheritdoc/>
    public override void RegisterServices(IServiceCollection services, ILoggerFactory? loggerFactory)
        => services.AddSingletonBroadcaster<
            ISchemaDiscoveryNotifier,
            SchemaDiscoveryNotifier,
            SchemaDiscoveryHub,
            ISchemaDiscoveryHubClient>(loggerFactory);

    /// <inheritdoc/>
    public override void Map(IEndpointRouteBuilder endpoints) => MapHubAt<SchemaDiscoveryHub>(endpoints);
}
