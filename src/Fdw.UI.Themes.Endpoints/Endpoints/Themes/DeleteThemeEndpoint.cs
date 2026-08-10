using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.UI.Themes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.Themes.Endpoints;

/// <summary>
/// Base endpoint to delete a UI theme. Delegates deletion to <see cref="ThemeConfigurationProvider"/>.
/// Subclasses may override <see cref="CanDelete"/> to add validation (e.g., reject deletion of the
/// default theme or built-in themes).
/// </summary>
public abstract class DeleteThemeEndpoint : Endpoint<DeleteThemeRequest>
{
    private readonly ThemeConfigurationProvider _provider;
    private readonly ILogger _logger;

    /// <inheritdoc />
    protected DeleteThemeEndpoint(
        ThemeConfigurationProvider provider,
        ILogger<DeleteThemeEndpoint>? logger = null)
    {
        _provider = provider;
        _logger = logger ?? NullLogger<DeleteThemeEndpoint>.Instance;
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

    /// <summary>Deletes the theme via the provider; 204 on success, 404 if missing, 400 if disallowed.</summary>
    public override async Task HandleAsync(DeleteThemeRequest req, CancellationToken ct)
    {
        if (!CanDelete(req.Name, out var reason))
        {
            if (string.Equals(reason, "not-found", System.StringComparison.Ordinal))
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }
            ThrowError(reason ?? "Cannot delete this theme", 400);
            return;
        }

        var result = await _provider.Delete(req.Name, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            ThrowError($"Theme delete failed: {string.Join("; ", result.Messages)}", 500);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Validation hook for subclasses (e.g., block deletion of built-in themes).</summary>
    protected virtual bool CanDelete(string name, out string? reason)
    {
        reason = null;
        return true;
    }
}
