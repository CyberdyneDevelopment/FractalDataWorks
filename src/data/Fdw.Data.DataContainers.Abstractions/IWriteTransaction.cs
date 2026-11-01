using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// Represents a transaction for atomic write operations.
/// </summary>
/// <remarks>
/// IWriteTransaction provides transactional capabilities for data writing,
/// ensuring that either all operations within the transaction succeed or
/// all are rolled back in case of failure.
/// </remarks>
public interface IWriteTransaction : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this transaction.
    /// </summary>
    /// <value>A unique transaction identifier for logging and debugging.</value>
    string TransactionId { get; }

    /// <summary>
    /// Gets a value indicating whether this transaction is still active.
    /// </summary>
    /// <value><c>true</c> if the transaction is active; otherwise, <c>false</c>.</value>
    bool IsActive { get; }

    /// <summary>
    /// Commits all operations performed within this transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the commit succeeded.</returns>
    /// <remarks>
    /// This method makes all changes within the transaction permanent.
    /// Once committed, the transaction becomes inactive and cannot be used further.
    /// </remarks>
    Task<IGenericResult> Commit(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back all operations performed within this transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the rollback succeeded.</returns>
    /// <remarks>
    /// This method discards all changes made within the transaction.
    /// Once rolled back, the transaction becomes inactive and cannot be used further.
    /// </remarks>
    Task<IGenericResult> Rollback(CancellationToken cancellationToken = default);
}