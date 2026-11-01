using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Abstractions.UnitOfWork;

/// <summary>
/// Runs an ordered list of data commands on one connection in one transaction: all of them take effect, or none
/// does. Returns one result: the total of the rows the steps affected, or the first failure.
/// </summary>
/// <remarks>
/// The commands are the ordinary data commands; nothing here names a table or column. A step's own failure
/// result is returned as it came (its code and details), after the transaction is rolled back. A failure to
/// start or commit the transaction is returned as a failure of the whole unit.
/// </remarks>
public sealed class DataUnitOfWork
{
    private readonly ILogger<DataUnitOfWork> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataUnitOfWork"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DataUnitOfWork(ILogger<DataUnitOfWork>? logger = null)
    {
        _logger = logger ?? NullLogger<DataUnitOfWork>.Instance;
    }

    /// <summary>
    /// Runs <paramref name="steps"/> in order inside one transaction on <paramref name="connection"/>.
    /// </summary>
    /// <param name="connection">The connection that owns the transaction.</param>
    /// <param name="steps">The steps, in the order they run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total rows affected, or the failure that rolled the unit back.</returns>
    public async Task<IGenericResult<int>> Run(
        ITransactionalDataConnection connection,
        IReadOnlyList<UnitOfWorkStep> steps,
        CancellationToken cancellationToken = default)
    {

        if (steps.Count == 0)
            return GenericResult<int>.Success(0);

        DataUnitOfWorkLog.Starting(_logger, steps.Count);
        var begun = await connection.BeginTransaction(cancellationToken).ConfigureAwait(false);
        if (!begun.IsSuccess || begun.Value is null)
            return GenericResult<int>.Failure(DataUnitOfWorkLog.BeginFailed(_logger));

        await using var transaction = begun.Value;
        var total = 0;
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            IGenericResult<int> result;
            try
            {
                result = await transaction.Execute<int>(step.Command, step.Container, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await RollBack(transaction, index + 1, cancellationToken).ConfigureAwait(false);
                throw;
            }

            if (!result.IsSuccess)
            {
                DataUnitOfWorkLog.StepFailed(_logger, index + 1, steps.Count, step.Command.CommandType, index);
                await RollBack(transaction, index + 1, cancellationToken).ConfigureAwait(false);
                return result;
            }

            total += result.Value;
        }

        var committed = await transaction.Commit(cancellationToken).ConfigureAwait(false);
        if (!committed.IsSuccess)
            return GenericResult<int>.Failure(DataUnitOfWorkLog.CommitFailed(_logger, steps.Count));

        DataUnitOfWorkLog.Committed(_logger, steps.Count, total);
        return GenericResult<int>.Success(total);
    }

    private async Task RollBack(IDataConnectionTransaction transaction, int stepNumber, CancellationToken cancellationToken)
    {
        var rolledBack = await transaction.Rollback(cancellationToken).ConfigureAwait(false);
        if (!rolledBack.IsSuccess)
            DataUnitOfWorkLog.RollbackFailed(_logger, stepNumber);
    }
}
