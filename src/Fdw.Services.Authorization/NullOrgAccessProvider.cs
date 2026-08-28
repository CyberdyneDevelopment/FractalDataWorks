using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Configuration;

namespace Fdw.Services.Authorization;

/// <summary>
/// Null-object org-access provider — contributes zero org grants.
/// Used when org access resolution is not wired in the DI container.
/// </summary>
public sealed class NullOrgAccessProvider : IOrgAccessProvider
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly NullOrgAccessProvider Instance = new();

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>> Get(
        Guid userId,
        Guid tenantId,
        Guid orgId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IGenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>>(
            GenericResult<IReadOnlyList<TenantOrgAccessConfiguration>>.Success(
                Array.Empty<TenantOrgAccessConfiguration>()));
}
