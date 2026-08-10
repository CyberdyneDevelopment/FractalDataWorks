using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that searches the catalog using free-text queries, entity type filters, and tags.</summary>
public abstract class SearchCatalogEndpoint : Endpoint<CatalogSearchRequest, List<CatalogEntryDto>>
{
    /// <summary>Gets the authorization policy required for read operations.</summary>
    protected virtual string ReadPolicy => "datastores:read";

    /// <summary>Gets the logger instance for this endpoint.</summary>
    protected new ILogger Logger { get; private set; } = null!;

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/catalog/search");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s => s.Summary = "Search the catalog");
    }

    /// <summary>Executes a catalog search using the provided criteria and returns matching entries.</summary>
    // Why: empty Query returns all catalog entries — the catalog must be browsable without a query.
    public override async Task HandleAsync(CatalogSearchRequest req, CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var results = await PerformSearch(req, ct).ConfigureAwait(false);
        await Send.OkAsync(results.ToList(), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the catalog search. Override to provide a custom search implementation that enumerates the
    /// host's catalog entities (datasets, containers, …). The default returns no entries.
    /// </summary>
    // Why: async so an override can enumerate datasets/containers through their async providers; the base
    // catalog assembly stays generic (no Data dependency) — the consuming app supplies the real source.
    protected virtual Task<IReadOnlyList<CatalogEntryDto>> PerformSearch(CatalogSearchRequest req, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<CatalogEntryDto>>([]);
}
