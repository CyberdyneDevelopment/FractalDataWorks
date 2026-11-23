using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// What the access rule needs to know about one tenant (FDW-6528): which tenant it is, whether it is active, and whether it is the global one.
/// </summary>
/// <param name="Id">The tenant's durable Id.</param>
/// <param name="IsActive">Whether the tenant is active.</param>
/// <param name="IsGlobal">Whether it is the global tenant, whose members may act in any tenant.</param>
public sealed record TenantFact(Guid Id, bool IsActive, bool IsGlobal);
