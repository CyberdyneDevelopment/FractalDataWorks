using System.Text.Json;
using FastEndpoints;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request to save the draft of a tenant's home page (FDW-2471). The body of the call is the page itself.
/// </summary>
public class SaveTenantHomePageDraftRequest : TenantHomePageRequest
{
    /// <summary>Gets or sets the page: the whole body of the request.</summary>
    [FromBody]
    public JsonElement Page { get; set; }
}
