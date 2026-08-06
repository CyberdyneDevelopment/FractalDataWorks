using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.SessionState;

/// <summary>
/// Service for managing per-user session state persistence.
/// Keys follow the format: {domain}:{page}:{component}.
/// </summary>
public interface ISessionStateService
{
    /// <summary>
    /// Saves a state value for the specified user and key.
    /// If a value already exists for the key, it is updated.
    /// </summary>
    /// <typeparam name="T">The type of the value to save.</typeparam>
    /// <param name="userId">The user identifier.</param>
    /// <param name="key">The state key in format {domain}:{page}:{component}.</param>
    /// <param name="value">The value to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult<bool>> SaveState<T>(string userId, string key, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a state value for the specified user and key.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="userId">The user identifier.</param>
    /// <param name="key">The state key in format {domain}:{page}:{component}.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized value, or null if not found.</returns>
    Task<IGenericResult<T?>> GetState<T>(string userId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a state value for the specified user and key.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="key">The state key in format {domain}:{page}:{component}.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult<bool>> DeleteState(string userId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all state keys for the specified user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of all state keys for the user.</returns>
    Task<IGenericResult<IReadOnlyList<string>>> GetAllKeys(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all state values for the specified user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult<bool>> ClearAll(string userId, CancellationToken cancellationToken = default);
}
