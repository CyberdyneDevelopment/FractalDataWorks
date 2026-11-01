using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// A place in the scope tree, named by its kind and id (FDW-2538).
/// </summary>
/// <param name="Kind">One of <see cref="PlaceKinds"/>.</param>
/// <param name="Id">The id of the tenant, organization or workspace.</param>
public sealed record PlaceRef(string Kind, Guid Id);
