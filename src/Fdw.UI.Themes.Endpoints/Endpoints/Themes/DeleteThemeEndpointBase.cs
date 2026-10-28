using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.UI.Themes;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.UI.Themes.Endpoints;

/// <summary>
/// Base endpoint to delete a UI theme. Delegates deletion to <see cref="ThemeConfigurationProvider.Remove"/>,
/// which refuses the default theme and a built-in theme.
/// </summary>
public abstract class DeleteThemeEndpointBase : Endpoint<DeleteThemeRequest>
{
    private readonly ThemeConfigurationProvider _provider;

    /// <inheritdoc />
    protected DeleteThemeEndpointBase(ThemeConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for routing and policies.</summary>
    protected virtual string ResourceName => "themes";

    /// <summary>Gets the authorization policy name for write operations.</summary>
    protected virtual string WritePolicy => "configurations:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Delete($"/{ResourceName}/{{Name}}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s =>
        {
            s.Summary = $"Delete a {ResourceName}";
            s.Description = $"Deletes a UI {ResourceName}. Cannot delete the default {ResourceName} or built-in {ResourceName}s.";
        });
    }

    /// <summary>Deletes the theme via the provider; 204 on success, otherwise the status of the refusal (404 missing, 409 default or built in).</summary>
    public override async Task HandleAsync(DeleteThemeRequest req, CancellationToken ct)
    {
        var removed = await _provider.Remove(req.Name, ct).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(removed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
