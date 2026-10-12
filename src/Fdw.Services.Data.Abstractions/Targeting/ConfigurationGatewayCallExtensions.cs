using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Routes a <see cref="DataGatewayCall"/> to the configuration gateway for a connection the host declares in its
/// <c>configurationSchema.json</c>.
/// </summary>
/// <remarks>
/// A store the host declares is reached only through its configuration gateway, never through the main data
/// gateway: the main gateway resolves data stores and connections from ConfigurationDb rows, which every caller with
/// a data permission can name, so a store kept out of those rows is one no user route can address (FDW-1084).
/// </remarks>
public static class ConfigurationGatewayCallExtensions
{
    /// <summary>
    /// Executes a <see cref="DataGatewayCall"/> against the configuration gateway for <paramref name="connectionName"/>.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="gateways">Holds the configuration gateways, one per declared connection.</param>
    /// <param name="connectionName">The declared connection whose gateway runs the call.</param>
    /// <param name="call">The bundled command and target.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result, or the failure that says the connection is not declared.</returns>
    public static async Task<IGenericResult<T>> Execute<T>(
        this IConfigurationGatewayProvider gateways,
        string connectionName,
        DataGatewayCall call,
        CancellationToken cancellationToken = default)
    {
        if (gateways is null) throw new ArgumentNullException(nameof(gateways));

        var gateway = gateways.Get(connectionName);
        if (gateway.IsFailure)
            return gateway.ToNewResult<T>();

        return await gateway.Value!.Execute<T>(call, cancellationToken).ConfigureAwait(false);
    }
}
