#pragma warning disable CS1591
using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Types.MsSql.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Types.MsSql;

/// <summary>
/// Extension methods for ensuring types schema exists during application startup.
/// </summary>
public static class TypesSchemaExtensions
{
    /// <summary>
    /// Ensures the types schema and tables exist in the database.
    /// </summary>
    /// <param name="host">The application host.</param>
    /// <param name="connectionStringName">Name of the connection string in configuration (default: "PlatformConfiguration").</param>
    /// <returns>The host for method chaining.</returns>
    public static async Task<IHost> EnsureTypesSchema(
        this IHost host,
        string connectionStringName = "PlatformConfiguration")
    {
        if (host == null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var loggerFactory = host.Services.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger(typeof(TypesSchemaExtensions));

        var connectionString = configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (logger != null)
            {
                TypesSchemaLog.ConnectionStringNotFound(logger, connectionStringName);
            }

            return host;
        }

        if (logger != null)
        {
            TypesSchemaLog.InitializingWithConnectionString(logger, connectionStringName);
        }

        var result = await TypesSchemaInitializer.EnsureTypesSchema(connectionString, logger).ConfigureAwait(false);

        if (!result.IsSuccess && logger != null)
        {
            TypesSchemaLog.SchemaDeploymentFailed(logger, string.Join(", ", result.Messages.Select(m => m.Message)));
        }

        return host;
    }

    /// <summary>
    /// Ensures the types schema and tables exist in the database using a specific connection string.
    /// </summary>
    /// <param name="host">The application host.</param>
    /// <param name="connectionString">The connection string to use.</param>
    /// <returns>The host for method chaining.</returns>
    public static async Task<IHost> EnsureTypesSchemaWithConnectionString(
        this IHost host,
        string connectionString)
    {
        if (host == null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        var loggerFactory = host.Services.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger(typeof(TypesSchemaExtensions));

        if (logger != null)
        {
            TypesSchemaLog.Initializing(logger);
        }

        var result = await TypesSchemaInitializer.EnsureTypesSchema(connectionString, logger).ConfigureAwait(false);

        if (!result.IsSuccess && logger != null)
        {
            TypesSchemaLog.SchemaDeploymentFailed(logger, string.Join(", ", result.Messages.Select(m => m.Message)));
        }

        return host;
    }
}
