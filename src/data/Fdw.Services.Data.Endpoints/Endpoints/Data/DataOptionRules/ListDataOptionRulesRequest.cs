using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The request that lists the data option rules of a container (FDW-6339).</summary>
public sealed class ListDataOptionRulesRequest : DataOptionContainerRequest
{
    /// <summary>Gets or sets the column to list (body); all columns when left out.</summary>
    public string? Column { get; set; }

    /// <summary>Gets or sets whether to list removed rules or current ones (body); it has no default.</summary>
    public bool? Removed { get; set; }

    /// <summary>Gets or sets the kind of owner to list (body): Organization or Workspace.</summary>
    public string? OwnerKind { get; set; }

    /// <summary>Gets or sets the owner to list (body).</summary>
    public Guid? OwnerId { get; set; }

    /// <summary>Gets or sets the page, from 1 (body).</summary>
    public int? Page { get; set; }

    /// <summary>Gets or sets the page size (body).</summary>
    public int? PageSize { get; set; }
}
