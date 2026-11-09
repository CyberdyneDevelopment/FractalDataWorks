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
/// The organization writer on a host that does not run the tenant service (FDW-4180); see <see cref="UnavailableTenantWriter"/>. Every call answers
/// a failure; the SQL multitenancy type's registration replaces this one where it is present.
/// </summary>
public sealed class UnavailableOrganizationWriter : IOrganizationWriter
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="UnavailableOrganizationWriter"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public UnavailableOrganizationWriter(ILogger<UnavailableOrganizationWriter>? logger = null)
    {
        _logger = logger ?? NullLogger<UnavailableOrganizationWriter>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<Guid>> Create(NewOrganization organization, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(organization);
        return Task.FromResult<IGenericResult<Guid>>(GenericResult<Guid>.Failure(UnavailableWritersLog.OrganizationWriterUnavailable(_logger, "made")));
    }

    /// <inheritdoc />
    public Task<IGenericResult> Update(Guid organizationId, OrganizationChange change, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(change);
        return Task.FromResult<IGenericResult>(GenericResult.Failure(UnavailableWritersLog.OrganizationWriterUnavailable(_logger, "changed")));
    }

    /// <inheritdoc />
    public Task<IGenericResult> Delete(Guid organizationId, CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult>(GenericResult.Failure(UnavailableWritersLog.OrganizationWriterUnavailable(_logger, "deleted")));
}
