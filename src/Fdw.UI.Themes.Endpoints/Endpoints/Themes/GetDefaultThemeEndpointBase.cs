using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes.Endpoints;

/// <summary>
/// Base endpoint to retrieve the current default theme configuration.
/// Subclasses must implement <see cref="LoadDefaultTheme"/> to provide the theme data.
/// </summary>
/// <typeparam name="TDetail">The theme detail DTO type.</typeparam>
public abstract class GetDefaultThemeEndpointBase<TDetail> : EndpointWithoutRequest<TDetail>
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
        Get($"/{ResourceName}/default");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = $"Get default {ResourceName}";
            s.Description = $"Returns the current default {ResourceName} configuration.";
        });
    }

    /// <summary>Returns the current default theme configuration.</summary>
    public override Task HandleAsync(CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var theme = LoadDefaultTheme();
        return Send.OkAsync(theme, ct);
    }

    /// <summary>Loads the default theme. Override in subclasses to provide theme data.</summary>
    protected virtual TDetail LoadDefaultTheme() => default!;
}
