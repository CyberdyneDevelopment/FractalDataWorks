using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Users.Services;

/// <summary>
/// The tenant writer on a host that does not run the tenant service (FDW-4180), for example a single-tenant host that maps the sign-in and tenant
/// endpoints. Only the SQL multitenancy type implements <see cref="ITenantWriter"/>; without this a host that lacks it fails at start with no
/// service registered. Every call answers a failure; the SQL type's registration replaces this one where it is present.
/// </summary>
public sealed class UnavailableTenantWriter : ITenantWriter
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="UnavailableTenantWriter"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public UnavailableTenantWriter(ILogger<UnavailableTenantWriter>? logger = null)
    {
        _logger = logger ?? NullLogger<UnavailableTenantWriter>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<Guid>> Create(NewTenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        return Task.FromResult<IGenericResult<Guid>>(GenericResult<Guid>.Failure(UnavailableWritersLog.TenantWriterUnavailable(_logger, "made")));
    }

    /// <inheritdoc />
    public Task<IGenericResult> Update(Guid tenantId, TenantChange change, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(change);
        return Task.FromResult<IGenericResult>(GenericResult.Failure(UnavailableWritersLog.TenantWriterUnavailable(_logger, "changed")));
    }

    /// <inheritdoc />
    public Task<IGenericResult> Delete(Guid tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult>(GenericResult.Failure(UnavailableWritersLog.TenantWriterUnavailable(_logger, "deleted")));

    /// <inheritdoc />
    public Task<IGenericResult> SetDeleted(Guid tenantId, bool isDeleted, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult>(GenericResult.Failure(UnavailableWritersLog.TenantWriterUnavailable(_logger, "marked deleted or restored")));
}
