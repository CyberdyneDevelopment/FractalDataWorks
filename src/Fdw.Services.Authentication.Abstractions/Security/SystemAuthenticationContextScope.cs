using System;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Brackets a block of host-bootstrap code with the explicit <see cref="SystemAuthenticationContext"/>
/// elevation on <see cref="IAuthenticationContextAccessor.Current"/>, then restores whatever value the
/// accessor held before entry on <see cref="Dispose"/> — the elevation never survives past the end of
/// the bracketed block.
/// </summary>
/// <remarks>
/// <para>
/// Why this exists: host bootstrap — loading <c>configurationSchema.json</c>, resolving
/// <c>IConfigurationGateway</c>'s own connection to ConfigurationDb, and running each domain's
/// <c>{Domain}Types.Initialize</c>/<c>{Domain}Provider.Initialize</c> — runs on the same
/// synchronous/awaited call chain as <c>Program.cs</c>'s startup code, before
/// <c>app.Run()</c>/<c>app.RunAsync()</c> starts accepting HTTP traffic. Those reads have no
/// <see cref="System.Security.Claims.ClaimsPrincipal"/> and no per-request <c>TenantId</c>, so
/// without an explicit elevation they resolve to the reserved deny-everywhere
/// <see cref="AuthConstants.NoAccessPrincipalId"/> principal (see
/// <c>MsSqlConnection.BuildSessionContextPlan</c> in <c>Fdw.Services.Connections.MsSql</c>) and the
/// app could not read its own connection/data-store catalog to boot.
/// </para>
/// <para>
/// Why this is safe — does NOT leak into request scope: <see cref="IAuthenticationContextAccessor.Current"/>
/// is <see cref="System.Threading.AsyncLocal{T}"/>-backed and flows only through the awaited
/// continuations of the SAME logical call flow that set it. Kestrel dispatches each accepted
/// connection on its OWN fresh logical flow — it is not a continuation of the <c>Program.cs</c> call
/// stack that invoked <c>app.RunAsync()</c> — so a value set here is never visible to a request
/// handler even without the explicit restore below. This type restores the PRIOR value (not merely
/// <c>null</c>) on <see cref="Dispose"/> as defense in depth: even if a future caller nested this
/// scope inside another ambient context, leaving the block can never leave a stale elevated value
/// behind. This is deliberately NOT a DI-registered default — registering
/// <see cref="SystemAuthenticationContext"/> as the accessor's default would make every scope
/// (including every HTTP request) inherit system elevation, which is exactly the hole this scope is
/// built to avoid.
/// </para>
/// <para>
/// Usage (entry-point <c>Program.cs</c>, in the application's own repo — outside this package):
/// <code>
/// var accessor = app.Services.GetRequiredService&lt;IAuthenticationContextAccessor&gt;();
/// using (new SystemAuthenticationContextScope(accessor))
/// {
///     ConnectionConfigurationProvider.Initialize(app.Services, loggerFactory);
///     DataStoreProvider.Initialize(app.Services, loggerFactory);
///     // ... every other {Domain}Types.Initialize / {Domain}Provider.Initialize that may read
///     // ConfigurationDb during bootstrap ...
/// }
/// // accessor.Current is restored here — the request pipeline starts with NO system elevation.
/// </code>
/// </para>
/// </remarks>
public sealed class SystemAuthenticationContextScope : IDisposable
{
    private readonly IAuthenticationContextAccessor _accessor;
    private readonly IAuthenticationContext? _previous;
    private bool _disposed;

    /// <summary>
    /// Enters the scope: saves <paramref name="accessor"/>'s current value and sets
    /// <see cref="IAuthenticationContextAccessor.Current"/> to a new <see cref="SystemAuthenticationContext"/>.
    /// </summary>
    /// <param name="accessor">The ambient accessor to elevate for the duration of this scope.</param>
    public SystemAuthenticationContextScope(IAuthenticationContextAccessor accessor)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _previous = accessor.Current;
        accessor.Current = new SystemAuthenticationContext();
    }

    /// <summary>
    /// Ends the system elevation, restoring the accessor's value from before this scope was entered.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _accessor.Current = _previous;
        _disposed = true;
    }
}
