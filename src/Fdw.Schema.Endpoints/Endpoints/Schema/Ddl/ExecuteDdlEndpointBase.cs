using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Schema.Clients.Models;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Schema.Endpoints.Ddl;

/// <summary>
/// Abstract base endpoint for executing DDL statements (e.g., creating tables) on a connection.
/// Implementers must override <see cref="ExecuteDdl"/> to provide the actual DDL execution logic.
/// </summary>
/// <remarks>
/// This base class handles request validation, error handling, and HTTP response formatting.
/// Subclasses implement connection resolution and DDL execution using the appropriate command types.
/// </remarks>
public abstract class ExecuteDdlEndpointBase : Endpoint<ExecuteDdlRequest, ExecuteDdlResponse>
{
    private readonly ILogger<ExecuteDdlEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteDdlEndpointBase"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected ExecuteDdlEndpointBase(ILogger<ExecuteDdlEndpointBase>? logger = null)
    {
        _logger = logger ?? NullLogger<ExecuteDdlEndpointBase>.Instance;
    }

    /// <summary>
    /// Gets the route for this endpoint. Default is "/connections/{Name}/execute-ddl".
    /// </summary>
    protected virtual string Route => "/connections/{Name}/execute-ddl";

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
            s.Summary = "Execute DDL";
            s.Description = "Creates a table on the specified connection using safe, parameterized DDL execution.";
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
    /// Handles the request by validating input and executing DDL.
    /// </summary>
    public override async Task HandleAsync(ExecuteDdlRequest req, CancellationToken ct)
    {
        SchemaEndpointLog.DiscoveringSchema(_logger, req.Name);

        if (string.IsNullOrWhiteSpace(req.TableName))
        {
            AddError("TableName is required");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        if (req.Columns.Count == 0)
        {
            AddError("At least one column is required");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        try
        {
            var result = await ExecuteDdl(req, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                await Send.OkAsync(new ExecuteDdlResponse
                {
                    Success = true,
                    Message = $"Table '{req.TableName}' created successfully"
                }, ct).ConfigureAwait(false);
            }
            else
            {
                await Send.ResponseAsync(new ExecuteDdlResponse
                {
                    Success = false,
                    Message = result.CurrentMessage ?? "DDL execution failed"
                }, 500, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            SchemaEndpointLog.SchemaOperationFailed(_logger, ex, "execute-ddl", req.Name);
            await Send.ResponseAsync(new ExecuteDdlResponse
            {
                Success = false,
                Message = "An internal error occurred during DDL execution."
            }, 500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes the DDL operation for the specified request.
    /// Implementers should resolve the connection, build the DDL command,
    /// and execute it via the data gateway.
    /// </summary>
    /// <param name="request">The DDL execution request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure of the DDL execution.</returns>
    protected abstract Task<IGenericResult> ExecuteDdl(ExecuteDdlRequest request, CancellationToken ct);
}
