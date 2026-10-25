using System;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Request naming a glossary term and the place a version of it is held at (FDW-2331).</summary>
public class GlossaryTermVersionPlaceRequest : GlossaryTermVersionsRequest
{
    /// <summary>Gets or sets the kind of place: Organization or Workspace.</summary>
    public string PlaceKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the id of the organization or workspace.</summary>
    public Guid PlaceId { get; set; }
}
