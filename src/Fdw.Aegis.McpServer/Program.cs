using System;
using System.Threading.Tasks;
using Fdw.Aegis.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Fdw.Aegis.McpServer;

/// <summary>
/// Entry point for the Aegis Gateway stdio MCP server (Phase 1: PreApproved commands only,
/// ConfigurationDb-free).
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        if (!builder.Configuration.GetSection("Serilog").Exists())
        {
            // NO FALLBACKS: appsettings.json ships alongside this app and declares the sink and the
            // levels. A missing section is a deployment defect — fail loud rather than inventing a
            // default log path. Written to stderr because this runs before any logger exists, and
            // stderr is the safe channel (stdout is the protocol).
            await Console.Error.WriteLineAsync(
                "Aegis MCP server: required 'Serilog' configuration section is missing from appsettings.json.")
                .ConfigureAwait(false);
            return 1;
        }

        var serilogLogger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(serilogLogger, dispose: true);

        var schema = AegisHostRegistration.LoadSchema("aegisSchema.json");

        // Phase 1a/1b (before Build). Why the results are checked: a phase that fails returns a
        // coded failure, and continuing past it builds a host whose secret managers are not
        // registered - which surfaces later as a secret that cannot be resolved, far from the cause.
        // Failing here names the phase that actually broke.
        var configured = AegisHostRegistration.Configure(builder, loggerFactory: null);
        if (configured.IsFailure)
        {
            await Console.Error.WriteLineAsync(
                "Aegis MCP server: host registration failed: " + (configured.CurrentMessage ?? string.Empty))
                .ConfigureAwait(false);
            return 1;
        }

        var registered = AegisHostRegistration.Register(builder, schema, loggerFactory: null);
        if (registered.IsFailure)
        {
            await Console.Error.WriteLineAsync(
                "Aegis MCP server: host registration failed: " + (registered.CurrentMessage ?? string.Empty))
                .ConfigureAwait(false);
            return 1;
        }

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<AegisToolService>();

        var app = builder.Build();

        // Phase 2 (after Build).
        var initialized = AegisHostRegistration.Initialize(app, loggerFactory: null);
        if (initialized.IsFailure)
        {
            await Console.Error.WriteLineAsync(
                "Aegis MCP server: host registration failed: " + (initialized.CurrentMessage ?? string.Empty))
                .ConfigureAwait(false);
            return 1;
        }

        AegisLog.ServerReady(
            app.Services.GetRequiredService<ILogger<AegisToolService>>(),
            schema.Commands.Count,
            schema.Connections.Count);

        await app.RunAsync().ConfigureAwait(false);
        return 0;
    }
}
