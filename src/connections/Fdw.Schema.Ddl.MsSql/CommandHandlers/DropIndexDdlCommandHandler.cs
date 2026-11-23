using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;

namespace Fdw.Schema.Ddl.MsSql.CommandHandlers;

/// <summary>Writes the SQL Server statement of a DropIndex command.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlDdlCommandHandlers), "DropIndex")]
public sealed class DropIndexDdlCommandHandler : MsSqlDdlCommandHandlerBase<DropIndexCommand>
{
    /// <summary>Initializes a new instance of the <see cref="DropIndexDdlCommandHandler"/> class.</summary>
    public DropIndexDdlCommandHandler()
        : base("DropIndex")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<string> Write(DropIndexCommand command) =>
        GenericResult<string>.Success(MsSqlDdlGenerator.GenerateDropIndex(command));

    /// <inheritdoc/>
    protected override IEnumerable<string?> NamesOf(DropIndexCommand command) =>
        [.. MsSqlDdlGenerator.Stated(command.SchemaName), command.TableName, command.IndexName];
}
