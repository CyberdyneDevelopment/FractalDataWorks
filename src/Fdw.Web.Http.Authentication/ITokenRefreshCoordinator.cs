namespace Fdw.Web.Http.Authentication;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Coordinates token refresh operations to prevent concurrent refresh calls.
/// When multiple callers request a refresh simultaneously, implementations ensure
/// only one refresh executes and the rest wait for or skip the operation.
/// </summary>
/// <remarks>
/// <para>
/// The default implementation (<see cref="DefaultTokenRefreshCoordinator"/>) uses
/// <see cref="SemaphoreSlim"/> with a timestamp cooldown. Custom implementations
/// can provide distributed coordination (e.g., Redis), custom cooldown logic,
/// or telemetry hooks.
/// </para>
/// <para>
/// Register a custom implementation before calling <c>AddWasmAuthentication()</c>
/// (which uses <c>TryAdd</c>) to override the default.
/// </para>
/// </remarks>
public interface ITokenRefreshCoordinator
{
    /// <summary>
    /// Executes the refresh function with coordination guarantees.
    /// If another caller recently completed a refresh within the cooldown window,
    /// implementations may return <c>true</c> without executing the function.
    /// </summary>
    /// <param name="refreshFunc">The function that performs the actual token refresh.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// <c>true</c> if a fresh token is available after this call (either from a new
    /// refresh or a recently completed one); otherwise, <c>false</c>.
    /// </returns>
    Task<bool> RefreshOnce(Func<CancellationToken, Task<bool>> refreshFunc, CancellationToken cancellationToken = default);
}
