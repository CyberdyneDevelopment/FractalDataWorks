using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Fdw.Web.RestEndpoints.Extensions;

/// <summary>
/// Writes a structured 404 JSON envelope that matches the API-62 contract:
/// <c>{ "errorCode": "NotFound", "messages": [ "..." ] }</c>. FastEndpoints'
/// <c>Send.NotFoundAsync()</c> emits an empty body with a text/plain content
/// type, which breaks clients (Newman tests) that always expect JSON.
/// </summary>
public static class NotFoundEnvelope
{
    /// <summary>
    /// Writes a structured 404 response with the supplied resource label and identifier.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="resource">Resource label, e.g. "SecretManager", "Pipeline".</param>
    /// <param name="identifier">The missing identifier (name or id).</param>
    /// <param name="ct">Cancellation token.</param>
    public static Task WriteNotFound(this HttpContext context, string resource, string identifier, CancellationToken ct)
    {
        // Through FastEndpoints' sender, not a raw write: it marks the response as sent, which is
        // what stops the endpoint from sending again -- a raw write left that to whether the body
        // had reached the wire yet.
        return context.Response.SendResultAsync(TypedResults.Json(new
        {
            errorCode = "NotFound",
            messages = new[] { $"{resource} '{identifier}' was not found." }
        }, statusCode: StatusCodes.Status404NotFound));
    }
}
