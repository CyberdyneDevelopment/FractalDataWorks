using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Hosting.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Hosting.Extensions;

public static class HealthEndpointExtensions
{
    /// <summary>
    /// Maps a standardized /health endpoint that returns the service name and status.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static void MapFrameworkHealthEndpoint(this WebApplication app, string serviceName)
    {
        app.MapGet("/health", () => Microsoft.AspNetCore.Http.Results.Ok(new
        {
            status = "healthy",
            service = serviceName,
            timestamp = DateTime.UtcNow
        })).ExcludeFromDescription();

        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Fdw.Hosting.Health");
        HostingLog.HealthEndpointMapped(logger, serviceName);
    }
}
