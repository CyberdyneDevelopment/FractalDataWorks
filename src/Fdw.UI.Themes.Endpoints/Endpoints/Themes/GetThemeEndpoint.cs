using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes.Endpoints;

/// <summary>
/// Base endpoint to retrieve a theme by name.
/// Subclasses must implement <see cref="FindTheme"/> to provide the theme data.
/// </summary>
/// <typeparam name="TDetail">The theme detail DTO type.</typeparam>
public abstract class GetThemeEndpoint<TDetail> : Endpoint<ThemeNameRequest, TDetail>
    where TDetail : class
{
    /// <summary>Gets the resource name used for routing and policies.</summary>
    protected virtual string ResourceName => "themes";

    /// <summary>Gets the authorization policy name for read operations.</summary>
    protected virtual string ReadPolicy => "configurations:read";

    /// <summary>Gets the logger instance. Resolved during HandleAsync.</summary>
    protected new ILogger Logger { get; private set; } = null!;

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get($"/{ResourceName}/{{Name}}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = $"Get {ResourceName} by name";
            s.Description = $"Returns the full configuration for a specific {ResourceName}.";
        });
    }

    /// <summary>Retrieves a theme by name, returning its configuration or 404 if not found.</summary>
    public override async Task HandleAsync(ThemeNameRequest req, CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var theme = FindTheme(req.Name);
        if (theme == null)
        {
            // Why: API-62 — structured 404 envelope so clients can parse errorCode/messages.
            HttpContext.Response.StatusCode = 404;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(
                new { errorCode = "NotFound", messages = new[] { $"themes '{req.Name}' was not found." } }, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(theme, ct).ConfigureAwait(false);
    }

    /// <summary>Finds a theme by name. Override to implement lookup logic.</summary>
    protected virtual TDetail? FindTheme(string name) => null;
}
