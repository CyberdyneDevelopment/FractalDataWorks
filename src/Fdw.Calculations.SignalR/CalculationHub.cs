using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Fdw.Calculations.Contracts.Hubs;
using Fdw.SignalR;

namespace Fdw.Calculations.SignalR;

/// <summary>
/// SignalR hub for real-time calculation notifications.
/// </summary>
/// <remarks>
/// Built on <see cref="RealTimeHubBase{TClient}"/>: lifecycle logging, the uniform subscribe/unsubscribe
/// contract, and the per-user auto-join are inherited. The calculation-specific verbs are thin
/// key-builders over the inherited contract so the wire surface is preserved.
/// </remarks>
[Authorize]
public class CalculationHub : RealTimeHubBase<ICalculationHubClient>
{
    /// <inheritdoc/>
    protected override string HubName => "Calculation";

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationHub"/> class.
    /// </summary>
    /// <param name="logger">The logger for hub lifecycle and subscription events.</param>
    public CalculationHub(ILogger<CalculationHub> logger)
        : base(logger)
    {
    }

    /// <inheritdoc/>
    /// <remarks>Joins the connection to its per-user group; skips (logged) when unauthenticated.</remarks>
    protected override Task OnJoin() => JoinAuthenticatedUserScope();

    /// <inheritdoc/>
    /// <remarks>
    /// A <c>calc:{id}</c> group is refused for everyone: no owner of a calculation is stored, so there is nothing to compare the caller's tenant to
    /// (FDW-2804). Nothing publishes into it on this hub today. When a calculation's owner can be read, the check is added here.
    /// </remarks>
    protected override bool CanJoin(string scopeKey)
        => base.CanJoin(scopeKey) && !scopeKey.StartsWith("calc:", StringComparison.Ordinal);

    /// <summary>
    /// Subscribes the connection to updates for a specific calculation.
    /// </summary>
    /// <param name="calculationId">The calculation ID to subscribe to.</param>
    /// <returns>A task representing the subscription operation.</returns>
    public Task SubscribeToCalculation(string calculationId) => Subscribe($"calc:{calculationId}");

    /// <summary>
    /// Unsubscribes the connection from updates for a specific calculation.
    /// </summary>
    /// <param name="calculationId">The calculation ID to unsubscribe from.</param>
    /// <returns>A task representing the unsubscription operation.</returns>
    public Task UnsubscribeFromCalculation(string calculationId) => Unsubscribe($"calc:{calculationId}");

    /// <summary>
    /// Subscribes the connection to all calculation updates (admin only).
    /// </summary>
    /// <returns>A task representing the subscription operation.</returns>
    [Authorize(Policy = "system:admin")]
    public Task SubscribeToAllCalculations() => JoinScope("all-calculations");
}
