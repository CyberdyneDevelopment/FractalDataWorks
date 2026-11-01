using System;

namespace Fdw.Services.Workspaces;

/// <summary>The place a definition is read or written for: a workspace the caller asked for, else the organization they act in, else their tenant (FDW-2331, FDW-2560).</summary>
/// <param name="Kind">One of <c>PlaceKinds</c>.</param>
/// <param name="Id">The id of the place.</param>
/// <param name="TenantId">The tenant the caller acts in, which the place must be in.</param>
public sealed record CallerPlace(string Kind, Guid Id, Guid TenantId);
