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

        // Why the logging story is file-based: MCP stdio framing owns stdout, so ANY provider that
        // writes there interleaves log text with JSON-RPC and breaks every client. Two things
        // enforce that here: (1) the configured sink is a file, and (2) Serilog.Sinks.Console is
        // deliberately NOT referenced by this project, so a "Console" sink named in configuration
        // cannot resolve even if someone adds one. Levels come from configuration too, so raising
        // verbosity to Debug/Verbose is a config edit rather than a code change.
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

        // Why: aegisSchema.json is the entire "directory" this standalone host needs — the
        // declared connections, secret managers, and commands. There is no ConfigurationDb
        // connection here (NEVER AddConfigurationGateway) — see AegisHostRegistration.Register.
        var schema = AegisHostRegistration.LoadSchema("aegisSchema.json");

        // Phase 1a/1b (before Build).
        AegisHostRegistration.Configure(builder, loggerFactory: null);
        AegisHostRegistration.Register(builder, schema, loggerFactory: null);

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<AegisToolService>();

        var app = builder.Build();

        // Phase 2 (after Build).
        AegisHostRegistration.Initialize(app.Services, schema, loggerFactory: null);

        // Why AegisLog rather than a raw stderr write: this is the server's one startup fact, and it
        // belongs in the same structured stream (carrying the same AEG-prefixed Code) as every other
        // Aegis message, so an operator greps one file instead of two channels.
        AegisLog.ServerReady(
            app.Services.GetRequiredService<ILogger<AegisToolService>>(),
            schema.Commands.Count,
            schema.Connections.Count);

        await app.RunAsync().ConfigureAwait(false);
        return 0;
    }
}
