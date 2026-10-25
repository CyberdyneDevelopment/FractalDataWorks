using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// One place at or above a place in the scope tree, and how far above it is (FDW-2512).
/// </summary>
/// <param name="Kind">One of <see cref="PlaceKinds"/>.</param>
/// <param name="Id">The id of the tenant, organization or workspace.</param>
/// <param name="Depth">0 for the place itself, 1 for its parent, and so on up to the tenant.</param>
public sealed record PlaceStep(string Kind, Guid Id, int Depth);
