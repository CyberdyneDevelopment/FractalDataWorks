using System;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// One version of a glossary term, at a place the caller may read (FDW-2331).
/// </summary>
/// <param name="PlaceKind">Tenant, Organization or Workspace.</param>
/// <param name="PlaceId">The id of the tenant, organization or workspace.</param>
/// <param name="Depth">How far above the caller's own place this place is: 0 for the caller's own place.</param>
/// <param name="Definition">The definition held at that place.</param>
/// <param name="Owner">The owner held at that place.</param>
/// <param name="Label">What to show: the term's name, followed for an organization or a workspace by its name in parentheses.</param>
/// <param name="IsBase">True for the term itself, which is the tenant's version.</param>
/// <param name="Applies">True for the nearest version to the caller, which is the one that applies to them.</param>
public sealed record GlossaryTermVersionResponse(string PlaceKind, Guid PlaceId, int Depth, string Definition, string Owner, string Label, bool IsBase, bool Applies);
