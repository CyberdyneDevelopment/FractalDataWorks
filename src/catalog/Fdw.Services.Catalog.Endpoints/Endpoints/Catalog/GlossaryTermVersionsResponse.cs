using System;
using System.Collections.Generic;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// The versions of a glossary term at the places the caller may read, nearest the caller first (FDW-2331).
/// </summary>
/// <param name="Id">The identity of the term.</param>
/// <param name="Name">The name of the term.</param>
/// <param name="PlaceKind">The kind of place the versions were listed for: Workspace, Organization, or Tenant.</param>
/// <param name="PlaceId">The id of that place.</param>
/// <param name="Versions">The versions, nearest first; the first is the one that applies.</param>
public sealed record GlossaryTermVersionsResponse(Guid Id, string Name, string PlaceKind, Guid PlaceId, IReadOnlyList<GlossaryTermVersionResponse> Versions);
