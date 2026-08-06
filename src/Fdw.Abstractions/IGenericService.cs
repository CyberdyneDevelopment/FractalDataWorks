using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Abstractions;

/// <summary>
/// Base interface for all services in the Fdw framework.
/// </summary>
/// <remarks>
/// This interface is in Fdw.Abstractions to avoid circular dependencies
/// with source generators, following the same pattern as IGenericCommand.
/// Extended versions with command support are in Fdw.Services.Abstractions.
/// </remarks>
public interface IGenericService
{
    /// <summary>
    /// Gets the unique identifier for this service instance.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of the service.
    /// </summary>
    string ServiceType { get; }

    /// <summary>
    /// Gets a value indicating whether the service is currently available for use.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Executes a data command against the connection.
    /// </summary>
    /// <typeparam name="T">The type of the result expected from the command.</typeparam>
    /// <param name="command">The data command to execute.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the command execution outcome and any data returned.</returns>
    Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default);
    /// <summary>
    /// Executes a data command against the connection.
    /// </summary>
    /// <param name="command">The data command to execute.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the command execution outcome and any data returned.</returns>
    Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default);
}
