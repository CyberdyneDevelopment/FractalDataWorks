namespace Fdw.Services.Data.Endpoints;

/// <summary>The request that lists the organizations or workspaces the caller may own a data option rule in (FDW-6339).</summary>
public sealed class ListDataOptionOwnersRequest
{
    /// <summary>Gets or sets the kind of owner to list (body): a name the tree's <c>ownerKinds</c> answers. It has no default.</summary>
    public string? Kind { get; set; }

    /// <summary>Gets or sets text the owner's name contains (body).</summary>
    public string? Search { get; set; }

    /// <summary>Gets or sets the page, from 1 (body).</summary>
    public int? Page { get; set; }

    /// <summary>Gets or sets the page size (body).</summary>
    public int? PageSize { get; set; }
}
