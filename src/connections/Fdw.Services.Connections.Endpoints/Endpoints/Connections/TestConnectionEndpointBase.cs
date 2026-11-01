using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Endpoint that tests connectivity to a configured connection by name.
/// Actually opens the connection, persists results to conn.Connection health columns
/// and records a health check entry in ops.ConnectionHealthCheck.
/// </summary>
public abstract class TestConnectionEndpointBase : Endpoint<TestConnectionRequest, TestConnectionResponse>
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly ConnectionConfigurationProvider _configProvider;
    private readonly IConnectionHealthService _healthService;
    private readonly ILogger<TestConnectionEndpointBase> _logger;

    /// <inheritdoc />
    protected TestConnectionEndpointBase(
        IConnectionProvider connectionProvider,
        ConnectionConfigurationProvider configProvider,
        IConnectionHealthService healthService,
        ILogger<TestConnectionEndpointBase> logger)
    {
        _connectionProvider = connectionProvider;
        _configProvider = configProvider;
        _healthService = healthService;
        _logger = logger ?? NullLogger<TestConnectionEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/connections/{Name}/test");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("connections:read");
#endif
        Summary(s =>
        {
            s.Summary = "Test a connection";
            s.Description = "Tests connectivity to a configured connection by name and persists the result.";
        });
    }

    /// <summary>Tests the named connection and returns a success or failure response.</summary>
    public override async Task HandleAsync(TestConnectionRequest req, CancellationToken ct)
    {
        var getResult = await _connectionProvider.Get(req.Name, ct).ConfigureAwait(false);

        if (!getResult.IsSuccess)
        {
            var response = new TestConnectionResponse
            {
                Name = req.Name,
                Success = false,
                Message = getResult.CurrentMessage ?? "Connection not found"
            };

            await Send.OkAsync(response, ct).ConfigureAwait(false);
            return;
        }

        var connection = getResult.Value!;

        var stopwatch = Stopwatch.StartNew();
        var testResult = await connection.TestConnection(ct).ConfigureAwait(false);
        stopwatch.Stop();

        var success = testResult.IsSuccess;
        var message = success ? "Connection successful" : testResult.CurrentMessage ?? "Connection test failed";
        var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;

        await RecordHealthCheck(req.Name, success, responseTimeMs, success ? null : message, ct).ConfigureAwait(false);

        await Send.OkAsync(new TestConnectionResponse
        {
            Name = req.Name,
            Success = success,
            Message = message
        }, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Records a health check entry in conn.ConnectionHealthCheck via <see cref="IConnectionHealthService"/>.
    /// Failures are logged but do not affect the test response. The probe result is NOT written back onto
    /// the connection configuration — health status is state, not versioned config (FDW-623).
    /// </summary>
    private async Task RecordHealthCheck(string connectionName, bool isHealthy, int responseTimeMs, string? errorMessage, CancellationToken ct)
    {
        var configResult = await _configProvider.Get(connectionName, ct).ConfigureAwait(false);

        if (!configResult.IsSuccess || configResult.Value is null)
        {
            return;
        }

        var config = configResult.Value;

        var recordResult = await _healthService.RecordHealthCheck(
            config.Id,
            connectionName,
            isHealthy,
            responseTimeMs,
            errorMessage,
            ct).ConfigureAwait(false);

        if (recordResult.IsSuccess)
        {
            ConnectionEndpointLog.HealthCheckRecorded(_logger, connectionName, isHealthy);
        }
        else
        {
            ConnectionEndpointLog.HealthCheckRecordFailed(_logger, connectionName, recordResult.CurrentMessage ?? "Record failed");
        }
    }
}
