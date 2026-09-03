using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Extension methods that route a <see cref="DataGatewayCall"/> to <see cref="IDataGateway"/>
/// or <see cref="IDataGatewayTransaction"/>, keeping call sites concise.
/// </summary>
public static class DataGatewayCallExtensions
{
    /// <summary>
    /// Executes a <see cref="DataGatewayCall"/> against the gateway, returning a typed result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="gateway">The gateway to execute against.</param>
    /// <param name="call">The bundled command and target.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    public static Task<IGenericResult<T>> Execute<T>(
        this IDataGateway gateway,
        DataGatewayCall call,
        CancellationToken cancellationToken = default)
        => gateway.Execute<T>(call.Command, call.Target, cancellationToken);

    /// <summary>
    /// Executes a <see cref="DataGatewayCall"/> inside the given transaction scope, returning a typed result.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="transaction">The active transaction scope.</param>
    /// <param name="call">The bundled command and target.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    public static Task<IGenericResult<T>> Execute<T>(
        this IDataGatewayTransaction transaction,
        DataGatewayCall call,
        CancellationToken cancellationToken = default)
        => transaction.Execute<T>(call.Command, call.Target, cancellationToken);

    /// <summary>
    /// Executes a <see cref="DataGatewayCall"/> inside the given transaction scope, returning a non-generic result.
    /// </summary>
    /// <param name="transaction">The active transaction scope.</param>
    /// <param name="call">The bundled command and target.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    public static Task<IGenericResult> Execute(
        this IDataGatewayTransaction transaction,
        DataGatewayCall call,
        CancellationToken cancellationToken = default)
        => transaction.Execute(call.Command, call.Target, cancellationToken);
}
