using Fdw.ServiceTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.DevSession.McpServer;

/// <summary>Stdio MCP server host for the development-session domain.</summary>
public static class Program
{
    /// <summary>Runs the server until its stdio transport closes.</summary>
    public static async Task<int> Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

        // Every discovered domain configures and registers itself, the session domain and its MCP server included
        // (DevSessionHosting, FDW-3957); the application registers nothing (FDW056). Each phase stops the host on failure.
        var configured = PlatformServices.Configure(builder);
        if (configured.IsFailure)
        {
            await Console.Error.WriteLineAsync($"Configure phase failed: {configured.CurrentMessage}").ConfigureAwait(false);
            return 1;
        }

        var registered = PlatformServices.Register(builder);
        if (registered.IsFailure)
        {
            await Console.Error.WriteLineAsync($"Register phase failed: {registered.CurrentMessage}").ConfigureAwait(false);
            return 1;
        }

        var host = builder.Build();
        var initialized = PlatformServices.Initialize(host);
        if (initialized.IsFailure)
        {
            await Console.Error.WriteLineAsync($"Initialize phase failed: {initialized.CurrentMessage}").ConfigureAwait(false);
            return 1;
        }

        await host.RunAsync().ConfigureAwait(false);
        return 0;
    }
}
