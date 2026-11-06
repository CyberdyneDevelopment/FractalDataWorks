using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Enables a disabled connection again and records why (FDW-3951). A person does this: a threshold disables a connection,
/// and only a person clears it.
/// </summary>
/// <remarks>
/// Enabling a connection that is not disabled changes nothing and answers success. A request without a reason is refused,
/// because the reason is what the history of the connection keeps.
/// </remarks>
public abstract class EnableConnectionEndpointBase : Endpoint<EnableConnectionRequest>
{
    private readonly ConnectionConfigurationProvider _connections;
    private readonly IConnectionDisabledStates _states;
    private readonly ILogger<EnableConnectionEndpointBase> _logger;

    /// <summary>Initializes a new instance of the <see cref="EnableConnectionEndpointBase"/> class.</summary>
    /// <param name="connections">Reads the connection by name.</param>
    /// <param name="states">Where the disabled state of a connection is kept.</param>
    /// <param name="logger">The logger.</param>
    protected EnableConnectionEndpointBase(
        ConnectionConfigurationProvider connections,
        IConnectionDisabledStates states,
        ILogger<EnableConnectionEndpointBase>? logger = null)
    {
        _connections = connections;
        _states = states;
        _logger = logger ?? NullLogger<EnableConnectionEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/connections/{Name}/enable");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("connections:write");
#endif
        Summary(s =>
        {
            s.Summary = "Enable a disabled connection";
            s.Description = "Clears the disabled state of a connection and records the reason; the connection is resolved again from then on.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Lets the concrete endpoint add its own tags and metadata.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(EnableConnectionRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Reason))
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "A reason is required to enable a connection" }, statusCode: StatusCodes.Status400BadRequest)).ConfigureAwait(false);
            return;
        }

        var connection = await _connections.Get(req.Name, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
        if (connection.IsFailure)
        {
            // The cause is in the log; the caller gets our sentence and never the database's.
            await Send.ResultAsync(TypedResults.Json(new { Error = "The connection could not be read" }, statusCode: StatusCodes.Status500InternalServerError)).ConfigureAwait(false);
            return;
        }

        if (connection.Value is null)
        {
            ConnectionEndpointLog.ConnectionNotFound(_logger, req.Name);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var enabled = await _states.Enable(connection.Value.Id, req.Reason, ct).ConfigureAwait(false);
        if (enabled.IsFailure)
        {
            // The answer is ours and never the database's (FDW-4193): 403 for a refusal by the row-level security policy, 500 for any other failure; the log has the cause.
            await Send.ResultAsync(TypedResults.Json(new { Error = EnableConnectionAnswers.SentenceOf(enabled) }, statusCode: EnableConnectionAnswers.StatusOf(enabled))).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
