using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data.DataSets;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for reordering transforms in a field mapping's transform chain.
/// Accepts the new ordering as a list of transform identifiers and updates ordinals accordingly.
/// </summary>
/// <remarks>
/// Why: FieldMappingTransform is a child record that does not implement IGenericConfiguration,
/// so ordinal updates go through ConfigurationSaveCommand via IDataGateway. The configuration
/// connection name and path are sourced from DataSetConfigurationProvider rather than
/// IConfigurationConnectionNameProvider (which is the anti-pattern being eliminated).
/// </remarks>
public abstract class ReorderFieldMappingTransformsEndpointBase : Endpoint<ReorderFieldMappingTransformsRequest>
{
    /// <summary>Gets the data gateway for executing queries and commands.</summary>
    protected IDataGateway DataGateway { get; }

    // Why: DataSetConfigurationProvider owns the DataStoreName and PathName for the
    // configuration database — eliminating IConfigurationConnectionNameProvider (anti-pattern).
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <summary>Gets the logger instance. Resolved during HandleAsync.</summary>
    protected new ILogger Logger { get; private set; } = null!;

    /// <inheritdoc />
    protected ReorderFieldMappingTransformsEndpointBase(
        IDataGateway dataGateway,
        DataSetConfigurationProvider dataSetProvider)
    {
        DataGateway = dataGateway;
        _dataSetProvider = dataSetProvider;
    }

    /// <summary>Gets the connection name for configuration database queries.</summary>
    protected virtual string ConfigurationConnectionName => _dataSetProvider.DataStoreName;

    /// <summary>
    /// Gets the configuration path that holds the transform containers.
    /// </summary>
    // Why not the dataset provider's PathName: these containers are declared in ConfigurationDb's
    // "transform" path, not "data". Borrowing the dataset path made every addressed lookup fail with
    // "DataContainer 'FieldMappingTransform' not found in path 'data'", which surfaced as a 404 on
    // every transform route. The container names beside this are literals for the same reason — the
    // endpoint names the location it targets rather than inheriting one that happens to differ.
    protected virtual string TransformPathName => "transform";

    /// <summary>Gets the container name for FieldMappingTransform queries.</summary>
    protected virtual string TransformContainerName => "FieldMappingTransform";

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/field-mappings/{FieldMappingId}/transforms/reorder");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:write");
#endif
        Summary(s =>
        {
            s.Summary = "Reorder field mapping transforms";
            s.Description = "Updates the ordinal positions of transforms in a field mapping's transform chain.";
        });

        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup
    /// such as caching, throttling, or tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ReorderFieldMappingTransformsRequest req, CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        try
        {
            var result = await ReorderTransforms(req.FieldMappingId, req.TransformIds, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
                HttpContext.Response.StatusCode = statusCode;
                await HttpContext.Response.WriteAsJsonAsync(errorResponse, ct).ConfigureAwait(false);
                return;
            }

            HttpContext.Response.StatusCode = StatusCodes.Status204NoContent;
        }
        catch (Exception ex)
        {
            EndpointLogger.EndpointError(Logger, ex, GetType().Name);
            HttpContext.Response.StatusCode = 500;
        }
    }

    /// <summary>
    /// Reorders transforms by updating their ordinal values to match the position in the provided list.
    /// Default implementation loads each transform and updates its ordinal via ConfigurationSaveCommand.
    /// Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult> ReorderTransforms(Guid fieldMappingId, IList<Guid> transformIds, CancellationToken ct)
    {
        FieldMappingTransformEndpointLog.ReorderingTransforms(Logger, fieldMappingId);

        // Load all current transforms for this field mapping.
        // Why: Addressing moved off IDataCommand onto DataStoreTarget.
        var queryCommand = new QueryCommand<FieldMappingTransformConfiguration>
        {
            Filter = DataSetQueryHelper.ByParentIdFilter(nameof(FieldMappingTransformConfiguration.DataSetFieldMappingId), fieldMappingId)
        };
        var loadResult = await DataGateway
            .Execute<IEnumerable<FieldMappingTransformConfiguration>>(
                queryCommand, new DataStoreTarget(ConfigurationConnectionName, TransformPathName, TransformContainerName), ct)
            .ConfigureAwait(false);
        if (loadResult.IsFailure)
        {
            FieldMappingTransformEndpointLog.ReorderTransformsFailed(Logger, fieldMappingId);
            return loadResult;
        }

        var transforms = loadResult.Value?.ToDictionary(t => t.Id) ?? new Dictionary<Guid, FieldMappingTransformConfiguration>();

        // Update ordinals based on the requested order
        for (var i = 0; i < transformIds.Count; i++)
        {
            var transformId = transformIds[i];
            if (!transforms.TryGetValue(transformId, out var transform))
            {
                FieldMappingTransformEndpointLog.TransformNotFound(Logger, transformId);
                continue;
            }

            transform.Ordinal = i;

            // Why: Addressing moved off IDataCommand onto DataStoreTarget.
            var saveResult = await DataGateway
                .Execute<int>(
                    new ConfigurationSaveCommand<FieldMappingTransformConfiguration>(transform),
                    new DataStoreTarget(ConfigurationConnectionName, TransformPathName, TransformContainerName),
                    ct)
                .ConfigureAwait(false);
            if (saveResult.IsFailure)
            {
                FieldMappingTransformEndpointLog.ReorderTransformsFailed(Logger, fieldMappingId);
                return saveResult;
            }
        }

        FieldMappingTransformEndpointLog.ReorderedTransforms(Logger, fieldMappingId);
        return GenericResult.Success();
    }
}
