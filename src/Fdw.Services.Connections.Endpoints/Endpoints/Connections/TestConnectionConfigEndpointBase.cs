using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Base endpoint for testing a connection configuration in-memory without persisting.
/// Used by the connection wizard to validate connectivity before saving.
/// </summary>
/// <typeparam name="TConfiguration">The connection configuration type.</typeparam>
public abstract class TestConnectionConfigEndpointBase<TConfiguration> : Endpoint<CreateConnectionRequest, TestConnectionResponse>
    // Why: typed body configs are standalone POCOs implementing IGenericConfiguration directly;
    // they no longer inherit from ConnectionConfiguration after the config-split refactor.
    where TConfiguration : class, IGenericConfiguration
{
    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected ILogger EndpointLogger { get; private set; } = null!;

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/connections/test-config");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("connections:read");
#endif
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override to configure endpoint-specific settings (summary, tags, etc.).
    /// </summary>
    protected abstract void ConfigureEndpoint();

    /// <summary>
    /// Creates a typed connection configuration from the request.
    /// </summary>
    protected abstract TConfiguration CreateConfiguration(CreateConnectionRequest request);

    /// <summary>
    /// Creates and tests a connection from the typed configuration.
    /// Returns a <see cref="TestConnectionResponse"/>.
    /// </summary>
    protected abstract Task<TestConnectionResponse> TestConfiguration(TConfiguration configuration, CancellationToken ct);

    /// <inheritdoc />
    public override async Task HandleAsync(CreateConnectionRequest req, CancellationToken ct)
    {
        EndpointLogger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        var name = req.Name ?? "Untitled";
        ConnectionEndpointLog.TestingConnectionConfig(EndpointLogger, name);

        var config = CreateConfiguration(req);
        var response = await TestConfiguration(config, ct).ConfigureAwait(false);

        if (response.Success)
        {
            ConnectionEndpointLog.ConnectionConfigTestSucceeded(EndpointLogger, name);
        }
        else
        {
            ConnectionEndpointLog.ConnectionConfigTestFailed(EndpointLogger, name, response.Message);
        }

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }
}
