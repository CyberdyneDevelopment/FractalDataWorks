using System;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// A version of a glossary term as it was saved at a place (FDW-2331).
/// </summary>
/// <param name="PlaceKind">Organization or Workspace.</param>
/// <param name="PlaceId">The id of the organization or workspace.</param>
/// <param name="Definition">The definition held at that place.</param>
/// <param name="Formula">The formula held at that place, if any.</param>
/// <param name="Owner">The owner held at that place.</param>
/// <param name="Steward">The steward held at that place, if any.</param>
public sealed record SavedGlossaryTermVersionResponse(string PlaceKind, Guid PlaceId, string Definition, string? Formula, string Owner, string? Steward);
