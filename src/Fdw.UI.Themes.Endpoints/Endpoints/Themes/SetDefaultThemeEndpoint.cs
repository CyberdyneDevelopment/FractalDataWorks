using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes.Endpoints;

/// <summary>
/// Endpoint to set a theme as the system default.
/// </summary>
public abstract class SetDefaultThemeEndpoint : Endpoint<SetDefaultThemeRequest>
{
    /// <summary>Gets the resource name used for routing and policies.</summary>
    protected virtual string ResourceName => "themes";

    /// <summary>Gets the authorization policy name for write operations.</summary>
    protected virtual string WritePolicy => "configurations:write";

    /// <summary>Gets the logger instance. Resolved during HandleAsync.</summary>
    protected new ILogger Logger { get; private set; } = null!;

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post($"/{ResourceName}/{{Name}}/default");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s =>
        {
            s.Summary = $"Set default {ResourceName}";
            s.Description = $"Sets a {ResourceName} as the system default.";
        });
    }

    /// <summary>Sets the specified theme as the system default, returning 204 on success or 404 if not found.</summary>
    public override async Task HandleAsync(SetDefaultThemeRequest req, CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        if (!ThemeExists(req.Name))
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        ApplyDefault(req.Name);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Checks whether a theme with the given name exists.</summary>
    protected virtual bool ThemeExists(string name) => false;

    /// <summary>Applies the theme as the system default. Override to implement the logic.</summary>
    protected virtual void ApplyDefault(string name) { }
}
