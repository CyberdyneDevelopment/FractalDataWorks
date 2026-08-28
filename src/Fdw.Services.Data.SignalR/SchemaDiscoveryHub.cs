using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Fdw.SignalR;

namespace Fdw.Services.Data.SignalR;

/// <summary>
/// SignalR hub for real-time schema discovery progress notifications.
/// </summary>
/// <remarks>
/// Built on <see cref="RealTimeHubBase{TClient}"/>: lifecycle logging, the uniform subscribe/unsubscribe
/// contract, and the per-user auto-join are inherited. The discovery-specific verbs are thin
/// key-builders over the inherited contract so the wire surface is preserved.
/// </remarks>
[Authorize]
public class SchemaDiscoveryHub : RealTimeHubBase<ISchemaDiscoveryHubClient>
{
    /// <inheritdoc/>
    protected override string HubName => "SchemaDiscovery";

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaDiscoveryHub"/> class.
    /// </summary>
    /// <param name="logger">The logger for hub lifecycle and subscription events.</param>
    public SchemaDiscoveryHub(ILogger<SchemaDiscoveryHub> logger)
        : base(logger)
    {
    }

    /// <inheritdoc/>
    /// <remarks>Joins the connection to its per-user group; skips (logged) when unauthenticated.</remarks>
    protected override Task OnJoin() => JoinAuthenticatedUserScope();

    /// <summary>
    /// Subscribes the connection to updates for a specific discovery operation.
    /// </summary>
    /// <param name="discoveryId">The discovery ID to subscribe to.</param>
    /// <returns>A task representing the subscription operation.</returns>
    public Task SubscribeToDiscovery(string discoveryId) => Subscribe($"discovery:{discoveryId}");

    /// <summary>
    /// Unsubscribes the connection from updates for a specific discovery operation.
    /// </summary>
    /// <param name="discoveryId">The discovery ID to unsubscribe from.</param>
    /// <returns>A task representing the unsubscription operation.</returns>
    public Task UnsubscribeFromDiscovery(string discoveryId) => Unsubscribe($"discovery:{discoveryId}");

    /// <summary>
    /// Subscribes the connection to all schema discovery updates (admin only).
    /// </summary>
    /// <returns>A task representing the subscription operation.</returns>
    [Authorize(Policy = "system:admin")]
    public Task SubscribeToAllDiscoveries() => JoinScope("all-discoveries");
}
