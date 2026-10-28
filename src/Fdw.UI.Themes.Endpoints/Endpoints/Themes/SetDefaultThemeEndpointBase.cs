using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes.Endpoints;

/// <summary>
/// Endpoint to set a theme as the system default.
/// </summary>
public abstract class SetDefaultThemeEndpointBase : Endpoint<SetDefaultThemeRequest>
{
    /// <summary>Initializes a new instance of the <see cref="SetDefaultThemeEndpointBase"/> class.</summary>
    protected SetDefaultThemeEndpointBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>Gets the resource name used for routing and policies.</summary>
    protected virtual string ResourceName => "themes";

    /// <summary>Gets the authorization policy name for write operations.</summary>
    protected virtual string WritePolicy => "configurations:write";

    /// <summary>Gets the logger instance. Resolved during HandleAsync.</summary>
    protected new ILogger Logger { get; }

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

    /// <summary>Sets the specified theme as the system default, returning 204 on success or the status of the refusal.</summary>
    public override async Task HandleAsync(SetDefaultThemeRequest req, CancellationToken ct)
    {
        var set = await SetDefault(req.Name, ct).ConfigureAwait(false);
        if (set.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(set, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Makes the named theme the system default. Implemented by subclasses.</summary>
    /// <param name="name">The theme to make the default.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success; a not-found failure when no theme carries the name; or the failure to write it.</returns>
    protected abstract Task<IGenericResult> SetDefault(string name, CancellationToken ct);
}
