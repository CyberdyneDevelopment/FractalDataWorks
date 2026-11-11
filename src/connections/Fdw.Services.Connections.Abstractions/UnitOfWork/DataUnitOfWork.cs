using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
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
        // The failure of a step is returned as the step gave it, so a caller reads the connection's own result.
        var (failure, affected, _) = await Steps(connection, steps, cancellationToken).ConfigureAwait(false);
        return failure ?? GenericResult<int>.Success(affected.Sum());
    }

    /// <summary>
    /// Runs <paramref name="steps"/> in order inside one transaction on <paramref name="connection"/> and answers what each affected (FDW-4956): a caller
    /// whose unit holds a bookkeeping step, such as the keys a load stages beside its rows, counts only the steps that write what it reports.
    /// </summary>
    /// <param name="connection">The connection that owns the transaction.</param>
    /// <param name="steps">The steps, in the order they run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows each step affected, in step order, or the failure that rolled the unit back.</returns>
    public async Task<IGenericResult<IReadOnlyList<int>>> RunEach(
        ITransactionalDataConnection connection,
        IReadOnlyList<UnitOfWorkStep> steps,
        CancellationToken cancellationToken = default)
    {
        var (failure, affected, _) = await Steps(connection, steps, cancellationToken).ConfigureAwait(false);
        return failure is null
            ? GenericResult<IReadOnlyList<int>>.Success(affected)
            : failure.ToNewResult<IReadOnlyList<int>>();
    }

    /// <summary>
    /// Runs <paramref name="steps"/> in order inside one transaction and answers what each affected and, for a step whose command reports its unmatched keys,
    /// its answer (FDW-4988). A step's <see cref="UnitOfWorkStep.CheckAnswer"/> that refuses its answer rolls the unit back.
    /// </summary>
    /// <param name="connection">The connection that owns the transaction.</param>
    /// <param name="steps">The steps, in the order they run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>What each step answered, or the failure that rolled the unit back.</returns>
    public async Task<IGenericResult<UnitOfWorkAnswers>> RunAnswering(
        ITransactionalDataConnection connection,
        IReadOnlyList<UnitOfWorkStep> steps,
        CancellationToken cancellationToken = default)
    {
        var (failure, affected, answers) = await Steps(connection, steps, cancellationToken).ConfigureAwait(false);
        return failure is null
            ? GenericResult<UnitOfWorkAnswers>.Success(new UnitOfWorkAnswers(affected, answers))
            : failure.ToNewResult<UnitOfWorkAnswers>();
    }

    // The one loop every answer shares: the failure that rolled the unit back, as the step or the transaction gave it, or what each step affected and answered.
    private async Task<(IGenericResult<int>? Failure, List<int> Affected, List<BulkWriteAnswer?> Answers)> Steps(
        ITransactionalDataConnection connection,
        IReadOnlyList<UnitOfWorkStep> steps,
        CancellationToken cancellationToken)
    {
        var affected = new List<int>(steps.Count);
        var answers = new List<BulkWriteAnswer?>(steps.Count);
        if (steps.Count == 0)
            return (null, affected, answers);

        DataUnitOfWorkLog.Starting(_logger, steps.Count);
        var begun = await connection.BeginTransaction(cancellationToken).ConfigureAwait(false);
        if (!begun.IsSuccess || begun.Value is null)
            return (GenericResult<int>.Failure(DataUnitOfWorkLog.BeginFailed(_logger)), affected, answers);

        await using var transaction = begun.Value;
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            (IGenericResult<int> Result, BulkWriteAnswer? Answer) ran;
            try
            {
                ran = await RunStep(transaction, step, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await RollBack(transaction, index + 1, cancellationToken).ConfigureAwait(false);
                throw;
            }

            if (!ran.Result.IsSuccess)
            {
                DataUnitOfWorkLog.StepFailed(_logger, index + 1, steps.Count, step.Command.CommandType, index);
                await RollBack(transaction, index + 1, cancellationToken).ConfigureAwait(false);
                return (ran.Result, affected, answers);
            }

            affected.Add(ran.Result.Value);
            answers.Add(ran.Answer);
        }

        var committed = await transaction.Commit(cancellationToken).ConfigureAwait(false);
        if (!committed.IsSuccess)
            return (GenericResult<int>.Failure(DataUnitOfWorkLog.CommitFailed(_logger, steps.Count)), affected, answers);

        DataUnitOfWorkLog.Committed(_logger, steps.Count, affected.Sum());
        return (null, affected, answers);
    }

    // One step: a command that reports its unmatched keys runs for its answer, which the step's check may refuse; every other command runs for its count.
    private static async Task<(IGenericResult<int> Result, BulkWriteAnswer? Answer)> RunStep(
        IDataConnectionTransaction transaction, UnitOfWorkStep step, CancellationToken cancellationToken)
    {
        if (step.Command is not IReportsUnmatchedKeys { UnmatchedKeySample: not null })
            return (await transaction.Execute<int>(step.Command, step.Container, cancellationToken).ConfigureAwait(false), null);

        var answered = await transaction.Execute<BulkWriteAnswer>(step.Command, step.Container, cancellationToken).ConfigureAwait(false);
        if (!answered.IsSuccess || answered.Value is not { } answer)
            return (answered.ToNewResult<int>(), null);

        var refused = step.CheckAnswer?.Invoke(answer);
        return refused is null
            ? (GenericResult<int>.Success((int)Math.Min(answer.RowsAffected, int.MaxValue)), answer)
            : (GenericResult<int>.Failure(refused), answer);
    }

    private async Task RollBack(IDataConnectionTransaction transaction, int stepNumber, CancellationToken cancellationToken)
    {
        var rolledBack = await transaction.Rollback(cancellationToken).ConfigureAwait(false);
        if (!rolledBack.IsSuccess)
            DataUnitOfWorkLog.RollbackFailed(_logger, stepNumber);
    }
}
