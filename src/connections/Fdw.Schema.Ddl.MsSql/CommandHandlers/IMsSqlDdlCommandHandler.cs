using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;

namespace Fdw.Schema.Ddl.MsSql.CommandHandlers;

/// <summary>Writes the SQL Server statement of one kind of DDL command and lists the names it writes into it.</summary>
public interface IMsSqlDdlCommandHandler : ITypeOption<int, IMsSqlDdlCommandHandler>
{
    /// <summary>Writes the statement for the command.</summary>
    /// <param name="command">The command, of the type this handler carries out.</param>
    /// <returns>The statement, or the refusal.</returns>
    IGenericResult<string> Generate(IDdlCommand command);

    /// <summary>Lists every name the command writes into SQL, each of which the dialect must be able to quote.</summary>
    /// <param name="command">The command, of the type this handler carries out.</param>
    /// <returns>The names.</returns>
    IEnumerable<string?> NamesIn(IDdlCommand command);
}
