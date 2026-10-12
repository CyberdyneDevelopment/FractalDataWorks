using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The shared part of the lineage endpoints: they read through the map's providers and access rules, and refuse as the map does.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class LineageEndpointBase<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    private protected LineageEndpointBase(
        ILogger logger,
        IDataSetConfigurationProvider dataSets,
        DataStoreConfigurationProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        IDataSetVariationConfigurationProvider variations,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IFrameworkAuthorizationService authorization)
    {
        LineageLogger = logger ?? NullLogger.Instance;
        Reader = new LineageReader(
            new MapSources(dataSets, dataStores, pipelines, variations),
            new MapScopeSelector(workspaces, access, LineageLogger),
            authorization,
            LineageLogger);
    }

    private protected ILogger LineageLogger { get; }

    private protected LineageReader Reader { get; }

    private protected void ConfigureLineage(string route, string summary, string description)
    {
        Get(route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = summary;
            s.Description = description;
        });
        Description(x => x.WithTags("lineage"));
    }

    private protected async Task Reply(IGenericResult<TResponse> result, CancellationToken ct)
    {
        if (result.IsFailure)
        {
            await Send.ResultAsync(MapProblemDetails.From(result, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
    }
}
