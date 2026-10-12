using Fdw.DevSession;
using Microsoft.Extensions.DependencyInjection;
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

        builder.Services.AddDevSessions();

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<DevSessionToolService>();

        await builder.Build().RunAsync().ConfigureAwait(false);
        return 0;
    }
}
