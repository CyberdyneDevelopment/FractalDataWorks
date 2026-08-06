using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes.Endpoints;

/// <summary>
/// Base endpoint to update an existing UI theme configuration.
/// Subclasses must implement lookup and update logic.
/// </summary>
/// <typeparam name="TRequest">The update theme request type.</typeparam>
/// <typeparam name="TDetail">The theme detail DTO type.</typeparam>
public abstract class UpdateThemeEndpoint<TRequest, TDetail> : Endpoint<TRequest, TDetail>
    where TRequest : notnull, new()
    where TDetail : class
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
        Put($"/{ResourceName}/{{Name}}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s =>
        {
            s.Summary = $"Update a {ResourceName}";
            s.Description = $"Updates an existing UI {ResourceName} configuration.";
        });
    }

    /// <summary>Updates the theme identified by name, returning the updated configuration or 404 if not found.</summary>
    public override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var name = GetThemeName(req);

        var existing = FindTheme(name);
        if (existing == null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var updated = ApplyUpdate(req, existing);
        await Send.OkAsync(updated, ct).ConfigureAwait(false);
    }

    /// <summary>Extracts the theme name from the request. Override to provide the name.</summary>
    protected virtual string GetThemeName(TRequest req) => string.Empty;

    /// <summary>Finds a theme by name. Override to implement lookup logic.</summary>
    protected virtual TDetail? FindTheme(string name) => null;

    /// <summary>Applies the update request to the existing theme. Override to implement merge logic.</summary>
    protected virtual TDetail ApplyUpdate(TRequest req, TDetail existing) => existing;
}
