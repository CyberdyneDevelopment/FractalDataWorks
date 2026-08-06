using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Sql.Workspace;

namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Non-generic SQL command translator surface used by the handler / registry.</summary>
public interface ISqlCommandTranslator
{
    /// <summary>The command type this translator handles.</summary>
    Type CommandType { get; }

    /// <summary>Executes the command against the workspace.</summary>
    Task<IGenericResult<ISqlCommandResult>> Execute(ISqlCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default);
}

/// <summary>Typed SQL command translator surface.</summary>
public interface ISqlCommandTranslator<in TCommand, TResult> : ISqlCommandTranslator
    where TCommand : ISqlCommand
    where TResult : ISqlCommandResult
{
    /// <summary>Strongly-typed entry point.</summary>
    Task<IGenericResult<TResult>> Translate(TCommand command, ISqlWorkspace workspace, CancellationToken cancellationToken = default);
}
