using System;

namespace Fdw.Services.Messaging;

/// <summary>
/// An alert a source found for the caller (FDW-2420): the identifier it was asked for and the tenant it belongs to, which every action on it is recorded in.
/// </summary>
public sealed class AlertSubject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlertSubject"/> class.
    /// </summary>
    /// <param name="id">The alert's identifier.</param>
    /// <param name="tenantId">The tenant the alert belongs to.</param>
    public AlertSubject(Guid id, Guid tenantId)
    {
        Id = id;
        TenantId = tenantId;
    }

    /// <summary>Gets the alert's identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the tenant the alert belongs to.</summary>
    public Guid TenantId { get; }
}
