using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Reads a data store's schema from its connection and saves what the store lacks:
/// <c>POST /datastores/-/discover</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every path, container, field and key the connection's schema discovery finds and the store does
/// not have is added, and the store is saved once (FDW-920). Nothing is removed, and a container the
/// store already has keeps its fields and keys - unless <c>refresh</c> is set, which brings its type and
/// its fields' data types and nullability in line with the source and adds the fields it lacks. See <see cref="ISchemaInformationService.DiscoverInto"/>.
/// A connection that does not discover its schema (its option is <c>None</c>) is refused, not
/// answered with an empty store.
/// </para>
/// <para>
/// This used to answer without discovering anything: it counted the paths the store already had and
/// saved nothing, so every container still had to be added by hand.
/// </para>
/// </remarks>
public abstract class DiscoverDataStoreEndpointBase : Endpoint<DiscoverDataStoreRequest, DiscoveryResultPayload>
{
    private readonly DataStoreConfigurationProvider _dataStores;
    private readonly ISchemaInformationService _schema;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DiscoverDataStoreEndpointBase"/> class.</summary>
    /// <param name="dataStores">Reads and saves the store.</param>
    /// <param name="schema">Discovers through the store's connection and adds what the store lacks.</param>
    /// <param name="logger">The logger.</param>
    protected DiscoverDataStoreEndpointBase(
        DataStoreConfigurationProvider dataStores,
        ISchemaInformationService schema,
        ILogger logger)
    {
        _dataStores = dataStores ?? throw new ArgumentNullException(nameof(dataStores));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected virtual string ResourceName => "datastores";

    /// <summary>Gets the authorization policy for write access.</summary>
    protected virtual string WritePolicy => $"{ResourceName}:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post($"/{ResourceName}/-/discover");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Discover a data store's schema and save it";
            s.Description = "Reads the schema of the data store's connection and adds every path, container, field and key "
                + "the store does not have, in one save. Nothing is removed; refresh also brings the containers the store "
                + "already has in line with the source (type, field data types and nullability, missing fields). "
                + "A connection that does not discover its schema is refused.";
        });
    }

    /// <summary>Handles the discovery request.</summary>
    public override async Task HandleAsync(DiscoverDataStoreRequest request, CancellationToken ct)
    {
        var result = await Discover(request, ct).ConfigureAwait(false);
        if (result.IsFailure || result.Value is not { } payload)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(result, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(payload, ct).ConfigureAwait(false);
    }

    private async Task<IGenericResult<DiscoveryResultPayload>> Discover(DiscoverDataStoreRequest request, CancellationToken ct)
    {
        var store = await _dataStores.Get(request.Name, ct).ConfigureAwait(false);
        if (store.IsFailure || store.Value is not { } found)
        {
            return GenericResult<DiscoveryResultPayload>.Failure(DataStoreEndpointLog.DataStoreNotFound(_logger, request.Name));
        }

        var imported = await _schema.DiscoverInto(found, OptionsFrom(request), request.Refresh, ct).ConfigureAwait(false);
        if (imported.IsFailure || imported.Value is not { } result)
        {
            return imported.ToNewResult<DiscoveryResultPayload>();
        }

        // Saved even when nothing was added: the discovery itself is recorded (LastDiscoveredAt).
        var saved = await _dataStores.Update(found, found.Domain, found.Implementation, found.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            return saved.ToNewResult<DiscoveryResultPayload>();
        }

        return GenericResult<DiscoveryResultPayload>.Success(PayloadFor(found, result, request.Refresh));
    }

    // A schema list the caller did not send leaves discovery's own defaults in place.
    private static DataStoreDiscoveryOptions OptionsFrom(DiscoverDataStoreRequest request)
    {
        var options = DataStoreDiscoveryOptions.Default;
        if (request.ExcludedSchemas is { } excluded)
            options.ExcludedSchemas = [.. excluded];
        if (request.IncludeOnlySchemas is { } includeOnly)
            options.IncludeOnlySchemas = [.. includeOnly];
        options.DiscoverViews = request.DiscoverViews;
        options.DiscoverIndexes = request.DiscoverIndexes;
        return options;
    }

    private static DiscoveryResultPayload PayloadFor(
        IDataStoreImplementationConfiguration store, DataStoreSchemaImportResult result, bool refreshed)
    {
        var containers = store.Paths.SelectMany(p => p.Containers).ToList();
        var messages = new List<string>
        {
            $"Added {result.PathsAdded} path(s), {result.ContainersAdded} container(s) and {result.FieldsAdded} field(s); updated {result.FieldsUpdated} field(s).",
        };
        if (result.ContainersDiscovered == 0)
            messages.Add("The connection's schema discovery found no containers.");

        return new DiscoveryResultPayload
        {
            DataStoreName = store.Name,
            PathCount = store.Paths.Count,
            ContainersDiscovered = result.ContainersDiscovered,
            ContainerCount = containers.Count,
            FieldsDiscovered = result.FieldsDiscovered,
            FieldCount = containers.Sum(c => c.Fields.Count),
            IntrospectedAt = DateTime.UtcNow,
            WasRefreshed = refreshed,
            Messages = messages,
        };
    }
}
