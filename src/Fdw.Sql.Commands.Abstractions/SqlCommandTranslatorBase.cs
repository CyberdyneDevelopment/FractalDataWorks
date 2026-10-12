using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Sql.Commands.Abstractions.Results;
using Fdw.Sql.Workspace;

namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Non-generic translator base. Each translator is itself a TypeOption (discoverable by name).</summary>
public abstract class SqlCommandTranslatorBase : TypeOptionBase<SqlCommandTranslatorBase>, ISqlCommandTranslator
{
    /// <summary>Sentinel ctor.</summary>
    protected SqlCommandTranslatorBase()
        : base(0, string.Empty, string.Empty, string.Empty, string.Empty, "SqlCommandTranslator")
    { }

    /// <summary>Initializes a new translator.</summary>
    protected SqlCommandTranslatorBase(string name, string description)
        : base(name, name, name, description, "SqlCommandTranslator")
    { }

    /// <inheritdoc/>
    public abstract Type CommandType { get; }

    /// <inheritdoc/>
    public abstract Task<IGenericResult<ISqlCommandResult>> Execute(ISqlCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default);
}

/// <summary>Typed translator base. Down-casts the command and dispatches to <see cref="Translate"/>.</summary>
public abstract class SqlCommandTranslatorBase<TCommand, TResult> : SqlCommandTranslatorBase, ISqlCommandTranslator<TCommand, TResult>
    where TCommand : ISqlCommand
    where TResult : ISqlCommandResult
{
    /// <inheritdoc/>
    public override Type CommandType => typeof(TCommand);

    /// <summary>Sentinel ctor.</summary>
    protected SqlCommandTranslatorBase() : base() { }

    /// <summary>Initializes a new typed translator.</summary>
    protected SqlCommandTranslatorBase(string name, string description) : base(name, description) { }

    /// <inheritdoc/>
    public override async Task<IGenericResult<ISqlCommandResult>> Execute(ISqlCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default)
    {
        if (command is not TCommand typed)
        {
            return GenericResult<ISqlCommandResult>.Failure(
                SqlResultCodes.CommandExecutionFailed,
                ResultDetails.Create("Expected", typeof(TCommand).Name).With("Actual", command.GetType().Name));
        }
        var result = await Translate(typed, workspace, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return GenericResult<ISqlCommandResult>.Failure(
                SqlResultCodes.CommandExecutionFailed,
                ResultDetails.Create("Message", result.CurrentMessage ?? "Unknown error"));
        }
        return GenericResult<ISqlCommandResult>.Success(result.Value!);
    }

    /// <inheritdoc/>
    public abstract Task<IGenericResult<TResult>> Translate(TCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default);
}
