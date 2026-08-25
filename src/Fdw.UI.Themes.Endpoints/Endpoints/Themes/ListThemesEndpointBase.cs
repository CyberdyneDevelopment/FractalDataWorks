using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes.Endpoints;

/// <summary>
/// Base endpoint to list all available UI themes.
/// Subclasses must implement <see cref="LoadThemes"/> to provide the theme data.
/// </summary>
/// <typeparam name="TSummary">The theme summary DTO type.</typeparam>
public abstract class ListThemesEndpointBase<TSummary> : EndpointWithoutRequest<PaginatedResponse<TSummary>>
    where TSummary : class
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
        Get($"/{ResourceName}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = $"List available {ResourceName}";
            s.Description = $"Returns a list of all available UI {ResourceName}.";
        });
    }

    /// <summary>Returns a list of all available themes.</summary>
    public override Task HandleAsync(CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        // Why: Newman/clients expect a paginated envelope {items, skip, take, totalCount, hasMore}
        // matching the response shape from /pipelines and other Crud-list endpoints.
        var items = LoadThemes();
        var list = items.ToList();
        return Send.OkAsync(PaginatedResponse<TSummary>.Create(list, 0, list.Count, list.Count), ct);
    }

    /// <summary>Loads all available themes. Override to provide theme data.</summary>
    protected virtual IReadOnlyList<TSummary> LoadThemes() => [];
}
