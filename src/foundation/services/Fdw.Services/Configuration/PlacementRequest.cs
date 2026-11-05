using System;

namespace Fdw.Services.Configuration;

/// <summary>
/// A new member of a domain that a placement is asked to place (FDW-3692).
/// </summary>
/// <param name="Kind">What the member is, as the provider that placed it names it (<see cref="IPlacedConfigurationProvider{TImplementationConfiguration}.PlacementKind"/>).</param>
/// <param name="Id">The member's durable id, already stamped onto its implementation.</param>
/// <param name="Name">The member's name.</param>
public sealed record PlacementRequest(string Kind, Guid Id, string Name);
