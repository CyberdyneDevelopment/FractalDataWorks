using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Graphs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Asks whether the signed-in caller of a graph save may write scripts (FDW-3676): the same policy the stored script routes apply to a write.
/// </summary>
public static class GraphScriptPermission
{
    /// <summary>Makes the question for the caller of a request.</summary>
    /// <param name="context">The request being served.</param>
    /// <returns>The question <see cref="GraphScriptAccess.Check"/> asks when a graph holds script text.</returns>
    public static Func<CancellationToken, Task<bool>> For(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return async _ =>
        {
            var authorization = context.RequestServices.GetRequiredService<IAuthorizationService>();
            var allowed = await authorization.AuthorizeAsync(context.User, null, GraphScriptAccess.WritePolicy).ConfigureAwait(false);
            return allowed.Succeeded;
        };
    }
}
