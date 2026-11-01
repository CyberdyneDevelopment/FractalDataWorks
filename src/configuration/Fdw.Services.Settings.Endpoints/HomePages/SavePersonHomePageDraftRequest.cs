using System.Text.Json;
using FastEndpoints;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request to save the draft of the caller's own home page (FDW-2471). The body of the call is the page itself; the page is the caller's, so the
/// request names nobody.
/// </summary>
public class SavePersonHomePageDraftRequest
{
    /// <summary>Gets or sets the page: the whole body of the request.</summary>
    [FromBody]
    public JsonElement Page { get; set; }
}
