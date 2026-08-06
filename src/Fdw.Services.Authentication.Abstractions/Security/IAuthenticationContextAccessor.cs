namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Ambient accessor for the <see cref="IAuthenticationContext"/> of the current logical call flow
/// (an HTTP request, or a background execution such as a dequeued pipeline run).
/// </summary>
/// <remarks>
/// <para>
/// Why an accessor instead of ctor-injecting <see cref="IAuthenticationContext"/> directly: the
/// consumer that needs per-execution tenant data — <c>MsSqlConnectionFactory</c> — is registered as a
/// DI <c>Singleton</c> (required so the three-phase <c>ServiceTypeOption</c> registration/eager-resolve
/// pattern can cache it for the app's lifetime). A <c>Scoped</c> <see cref="IAuthenticationContext"/>
/// ctor-injected into a <c>Singleton</c> is a captive dependency — the DI container either throws
/// (`ValidateScopes`) or silently freezes whatever was resolved once at construction, for the life of
/// the process. Registering this accessor itself as a <c>Singleton</c> whose <c>Current</c> value is
/// backed by <see cref="System.Threading.AsyncLocal{T}"/> sidesteps that entirely — the same pattern
/// ASP.NET Core's own <c>IHttpContextAccessor</c> uses to let a singleton see per-request ambient state
/// safely. A value set inside one logical async flow (one HTTP request pipeline, or one background
/// execution's <c>ProcessRequest</c> call) is visible to everything awaited from that point onward, and
/// is NOT visible to concurrent or subsequent flows — no scope, no leakage, no captive dependency.
/// </para>
/// </remarks>
public interface IAuthenticationContextAccessor
{
    /// <summary>
    /// Gets or sets the <see cref="IAuthenticationContext"/> for the current logical call flow.
    /// Null when no context has been established (e.g. a startup/system connection created outside
    /// any request or execution flow).
    /// </summary>
    IAuthenticationContext? Current { get; set; }
}
