using Microsoft.AspNetCore.Builder;

namespace Fdw.VsCodeShell.Hosting;

/// <summary>Application-builder extensions that activate the VS Code shell middleware.</summary>
public static class VsCodeShellApplicationBuilderExtensions
{
    /// <summary>
    /// Activates the VS Code shell middleware. Must be called before terminal middleware
    /// (MapControllers / static files / Blazor) so <c>/vscode/*</c> routes are claimed.
    /// </summary>
    public static IApplicationBuilder UseVsCodeShell(this IApplicationBuilder app)
    {
        return app.UseMiddleware<VsCodeShellMiddleware>();
    }
}
