using System;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Supplies who is asking to switch tenant, and which tenant they are asking for.
/// </summary>
/// <remarks>
/// A seam rather than parameters, because a step's signature is fixed by the pipeline. A host binds
/// this to wherever the request actually arrives — the authenticated caller of an HTTP endpoint and
/// the tenant named in its body — without the step knowing which.
/// </remarks>
public interface ITenantSwitchRequestAccessor
{
    /// <summary>
    /// Gets the principal already authenticated on this request — the one asking to switch — or null
    /// when the request carries no authenticated principal.
    /// </summary>
    Guid? CallerId { get; }

    /// <summary>Gets the tenant the caller asks to act in, or null when none was named.</summary>
    Guid? RequestedTenantId { get; }

    /// <summary>
    /// Gets the organization (department) of that tenant the caller asks to act in, or null when the request names none
    /// (a switch of tenant alone, which leaves the token without a department; FDW-1802).
    /// </summary>
    Guid? RequestedOrganizationId { get; }
}
