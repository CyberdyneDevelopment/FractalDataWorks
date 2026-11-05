using System;
using System.Collections.Generic;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Makes the writes of a block land in a stated tenant, and puts the caller's authentication context back afterwards (FDW-3668).
/// </summary>
/// <remarks>
/// A write through a configuration provider is stamped with the tenant the database session is in, which comes from the authentication
/// context. Founding a tenant happens in the caller's own tenant, so the new tenant's default workspace would land in the wrong one. The
/// platform's way of acting in a tenant is a <see cref="TenantRunAuthenticationContext"/> (the pipeline runner does the same), so this
/// swaps one in for the length of the block, and only when the caller is not already acting in that tenant.
/// </remarks>
public sealed class TenantActing : IDisposable
{
    private const string PrincipalName = "default-workspace-founding";

    private readonly IAuthenticationContextAccessor _accessor;
    private readonly IAuthenticationContext? _caller;
    private bool _swapped;

    private TenantActing(IAuthenticationContextAccessor accessor)
    {
        _accessor = accessor;
        _caller = accessor.Current;
    }

    /// <summary>Starts acting in the tenant.</summary>
    /// <param name="accessor">The accessor the providers read the caller's authentication context from.</param>
    /// <param name="tenantId">The tenant the writes are for.</param>
    /// <param name="principalId">The person the run is made as.</param>
    /// <param name="logger">The caller's logger.</param>
    /// <returns>The scope; dispose it to put the caller's context back.</returns>
    public static TenantActing In(IAuthenticationContextAccessor accessor, Guid tenantId, Guid principalId, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(logger);

        var acting = new TenantActing(accessor);
        if (acting._caller?.ActiveTenantId == tenantId) return acting;

        DefaultWorkspaceLog.ActingInOtherTenant(logger, tenantId, acting._caller?.ActiveTenantId.ToString() ?? string.Empty);
        accessor.Current = new TenantRunAuthenticationContext(principalId, PrincipalName, tenantId, Array.Empty<string>(), Array.Empty<string>());
        acting._swapped = true;
        return acting;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_swapped) return;

        _accessor.Current = _caller;
        _swapped = false;
    }
}
