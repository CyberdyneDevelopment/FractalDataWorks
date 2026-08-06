using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Interface for connections capable of executing data commands with container metadata.
/// Extends IGenericConnection - uses base Execute methods.
/// </summary>
/// <remarks>
/// <para>
/// IDataConnection accepts IDataCommand with IStorageContainer metadata passed by DataGateway.
/// </para>
/// <para>
/// Implementations use IDataCommandTranslator internally to convert IDataCommand to IConnectionCommand.
/// </para>
/// </remarks>
public interface IDataConnection : IGenericConnection
{
    /// <summary>
    /// Executes a data command against the unified container.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="command">The data command to execute.</param>
    /// <param name="container">The unified container node (provided by DataGateway). Because
    /// <see cref="IDataContainer"/> derives from <see cref="IStorageContainer"/>, the connection
    /// reads schema, physical location, format, metadata, and keys off this one type.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the typed execution outcome.</returns>
    Task<IGenericResult<T>> Execute<T>(IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a data command against the unified container.
    /// </summary>
    /// <param name="command">The data command to execute.</param>
    /// <param name="container">The unified container node (provided by DataGateway). Because
    /// <see cref="IDataContainer"/> derives from <see cref="IStorageContainer"/>, the connection
    /// reads schema, physical location, format, metadata, and keys off this one type.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the execution outcome.</returns>
    Task<IGenericResult> Execute(IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a data command and materializes the result rows as objects of <paramref name="elementType"/>.
    /// </summary>
    /// <remarks>
    /// Reflection-free alternative to <c>Execute&lt;IEnumerable&lt;T&gt;&gt;</c> when the element type is only
    /// known at runtime (e.g. the configuration cascade loading a typed child collection): the connection
    /// picks the element's generated mapper by type and returns the rows as objects, so the caller does
    /// not close a generic with <c>MakeGenericMethod</c>.
    /// </remarks>
    /// <param name="command">The data command to execute.</param>
    /// <param name="container">The unified container node (provided by DataGateway).</param>
    /// <param name="elementType">The element type whose generated mapper materializes each row.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the materialized rows as objects.</returns>
    Task<IGenericResult<IEnumerable<object>>> Execute(IDataCommand command, IDataContainer container, Type elementType, CancellationToken cancellationToken = default);
}
