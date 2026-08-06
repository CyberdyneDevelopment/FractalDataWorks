using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes.Endpoints;

/// <summary>
/// Base endpoint to create a new UI theme configuration.
/// Validates uniqueness by name before creating.
/// </summary>
/// <typeparam name="TRequest">The create theme request type.</typeparam>
/// <typeparam name="TDetail">The theme detail DTO type.</typeparam>
public abstract class CreateThemeEndpoint<TRequest, TDetail> : Endpoint<TRequest, TDetail>
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
        Post($"/{ResourceName}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s =>
        {
            s.Summary = $"Create a new {ResourceName}";
            s.Description = $"Creates a new UI {ResourceName} configuration.";
        });
    }

    /// <summary>Creates a new theme after verifying that no theme with the same name exists.</summary>
    public override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var name = GetThemeName(req);

        if (ThemeExists(name))
        {
            ThrowError($"A {ResourceName} with this name already exists", 409);
            return;
        }

        var theme = CreateTheme(req);
        await Send.ResponseAsync(theme, 201, ct).ConfigureAwait(false);
    }

    /// <summary>Extracts the theme name from the request. Override to provide the name.</summary>
    protected virtual string GetThemeName(TRequest req) => string.Empty;

    /// <summary>Checks whether a theme with the given name already exists.</summary>
    protected virtual bool ThemeExists(string name) => false;

    /// <summary>Creates a new theme from the request. Override to implement creation logic.</summary>
    protected virtual TDetail CreateTheme(TRequest req) => default!;
}
