using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Schema.Clients.Models;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// Abstract base endpoint for importing database schema into DataStore configuration.
/// Implementers must override <see cref="ImportSchema"/> to provide the actual
/// discovery and persistence logic.
/// </summary>
/// <remarks>
/// This base class handles request validation, logging, error handling, and HTTP response
/// formatting. Subclasses implement connection resolution, schema discovery,
/// and DataStore/DataPath/DataContainer/Field persistence.
/// </remarks>
public abstract class ImportSchemaEndpointBase : Endpoint<ImportSchemaRequest, ImportSchemaResponse>
{
    private readonly ILogger<ImportSchemaEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportSchemaEndpointBase"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected ImportSchemaEndpointBase(ILogger<ImportSchemaEndpointBase>? logger = null)
    {
        _logger = logger ?? NullLogger<ImportSchemaEndpointBase>.Instance;
    }

    /// <summary>
    /// Gets the route for this endpoint. Default is "/connections/{ConnectionName}/import-schema".
    /// </summary>
    protected virtual string Route => "/connections/{ConnectionName}/import-schema";

    /// <summary>
    /// Gets the policy name for authorization. Default is "schema:write".
    /// </summary>
    protected virtual string PolicyName => "schema:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post(Route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies(PolicyName);
#endif
        Summary(s =>
        {
            s.Summary = "Import schema to DataStore";
            s.Description = "Discovers database schema and persists it as DataStore configuration (DataPaths, DataContainers, Fields).";
        });

        OnBeforeConfiguring();
    }

    /// <summary>
    /// Virtual hook called at the end of <see cref="Configure"/>.
    /// Override to add additional endpoint configuration.
    /// </summary>
    protected virtual void OnBeforeConfiguring()
    {
    }

    /// <summary>
    /// Handles the request by validating input and performing schema import.
    /// </summary>
    public override async Task HandleAsync(ImportSchemaRequest req, CancellationToken ct)
    {
        var dataStoreName = req.DataStoreName ?? req.ConnectionName;
        SchemaEndpointLog.ImportingSchema(_logger, req.ConnectionName, dataStoreName);

        try
        {
            await OnImporting(req, ct).ConfigureAwait(false);

            var result = await ImportSchema(req, ct).ConfigureAwait(false);

            if (!result.IsSuccess || result.Value == null)
            {
                var response = new ImportSchemaResponse
                {
                    Success = false,
                    DataStoreName = dataStoreName,
                    ErrorMessage = result.CurrentMessage ?? "Schema import failed"
                };

                await Send.ResponseAsync(response, 400, ct).ConfigureAwait(false);
                return;
            }

            var importResponse = result.Value;
            SchemaEndpointLog.SchemaImported(
                _logger,
                req.ConnectionName,
                importResponse.TablesImported + importResponse.ViewsImported,
                importResponse.ColumnsImported);

            await OnImported(importResponse, ct).ConfigureAwait(false);
            await Send.OkAsync(importResponse, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SchemaEndpointLog.SchemaOperationFailed(_logger, ex, "import", req.ConnectionName);
            var errorResponse = new ImportSchemaResponse
            {
                Success = false,
                DataStoreName = dataStoreName,
                ErrorMessage = "An internal error occurred during schema import."
            };

            await Send.ResponseAsync(errorResponse, 500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Performs schema import for the specified connection.
    /// Implementers should resolve the connection, discover its schema,
    /// and persist the results as DataStore configuration.
    /// </summary>
    /// <param name="request">The import request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the import response, or failure information.</returns>
    protected abstract Task<IGenericResult<ImportSchemaResponse>> ImportSchema(
        ImportSchemaRequest request,
        CancellationToken ct);

    /// <summary>
    /// Virtual hook called before import starts.
    /// Override to add custom validation or pre-processing logic.
    /// </summary>
    /// <param name="request">The import request.</param>
    /// <param name="ct">Cancellation token.</param>
    protected virtual Task OnImporting(ImportSchemaRequest request, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Virtual hook called after import succeeds, before sending the response.
    /// Override to add post-processing such as cache invalidation or notifications.
    /// </summary>
    /// <param name="response">The import response.</param>
    /// <param name="ct">Cancellation token.</param>
    protected virtual Task OnImported(ImportSchemaResponse response, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
