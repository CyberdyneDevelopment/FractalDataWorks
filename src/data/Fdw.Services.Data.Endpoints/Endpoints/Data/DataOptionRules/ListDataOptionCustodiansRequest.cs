
namespace Fdw.Services.Data.Endpoints;

/// <summary>The request that lists who may be a data option rule's custodian (FDW-6339).</summary>
public sealed class ListDataOptionCustodiansRequest
{
    /// <summary>Gets or sets text the person's name contains (body).</summary>
    public string? Search { get; set; }

    /// <summary>Gets or sets the page, from 1 (body).</summary>
    public int? Page { get; set; }

    /// <summary>Gets or sets the page size (body).</summary>
    public int? PageSize { get; set; }
}
