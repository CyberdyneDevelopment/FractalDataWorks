using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>
/// A local identity, resolved from a <see cref="Subject"/>.
/// </summary>
/// <remarks>
/// Resolution is always yours. No external party can say which of your principals an external
/// (issuer, subject) pair belongs to — that mapping is the federation boundary itself, and it stays
/// internal however much of the rest is delegated.
/// <para>
/// The tenant arrives here rather than at flow selection: a flow is chosen before anyone is known,
/// so tenant cannot gate it without circularity.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record Principal
{
    /// <summary>Gets the local principal identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the tenant this principal belongs to.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>
    /// Gets the organization (department) the token is to act in, or null when none is stated, in which
    /// case the token carries none and the request pipeline resolves the tenant's default (FDW-1802).
    /// </summary>
    public Guid? OrganizationId { get; init; }
}
