using System;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Request naming a glossary term whose versions are listed (FDW-2331).</summary>
public class GlossaryTermVersionsRequest
{
    /// <summary>Gets or sets the identity of the glossary term.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the workspace the versions are listed for. Left out, they are listed for the organization the caller acts in, or for the tenant when
    /// they act in none. A workspace of another tenant is refused as not found.
    /// </summary>
    public Guid? WorkspaceId { get; set; }
}
