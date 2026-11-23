using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Ddl.Results;

namespace Fdw.Schema.Ddl.MsSql.CommandHandlers;

/// <summary>The type the DDL handler collection is built on: one concrete base every handler shares, carrying the Id and Name the collection indexes by.</summary>
[ExcludeFromCodeCoverage]
public abstract class MsSqlDdlCommandHandlerBase : TypeOptionBase<MsSqlDdlCommandHandlerBase>, IMsSqlDdlCommandHandler
{
    /// <summary>Initializes the handler with the identity the collection indexes it by.</summary>
    /// <param name="name">The command type name (for example <c>CreateTable</c>).</param>
    protected MsSqlDdlCommandHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public abstract IGenericResult<string> Generate(IDdlCommand command);

    /// <inheritdoc/>
    public abstract IEnumerable<string?> NamesIn(IDdlCommand command);
}

/// <summary>A handler for one DDL command type.</summary>
/// <typeparam name="TCommand">The command this handler carries out.</typeparam>
[ExcludeFromCodeCoverage]
public abstract class MsSqlDdlCommandHandlerBase<TCommand> : MsSqlDdlCommandHandlerBase
    where TCommand : class, IDdlCommand
{
    /// <summary>Initializes the handler with the identity the collection indexes it by.</summary>
    /// <param name="name">The command type name.</param>
    protected MsSqlDdlCommandHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public sealed override IGenericResult<string> Generate(IDdlCommand command) =>
        command is TCommand typed
            ? Write(typed)
            : GenericResult<string>.Failure(
                DdlResultCodes.ByName("CommandGenerationFailed"),
                ResultDetails.Create("commandType", command.CommandType.ToString(), "error", $"Command type {command.CommandType} is not the type its handler writes"));

    /// <inheritdoc/>
    public sealed override IEnumerable<string?> NamesIn(IDdlCommand command) =>
        command is TCommand typed ? NamesOf(typed) : [];

    /// <summary>Writes the statement.</summary>
    /// <param name="command">The command.</param>
    /// <returns>The statement, or the refusal.</returns>
    protected abstract IGenericResult<string> Write(TCommand command);

    /// <summary>Lists every name the command writes into SQL.</summary>
    /// <param name="command">The command.</param>
    /// <returns>The names.</returns>
    protected abstract IEnumerable<string?> NamesOf(TCommand command);
}
