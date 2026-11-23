using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;

namespace Fdw.Schema.Ddl.MsSql.CommandHandlers;

/// <summary>Writes the SQL Server statement of a DropTable command.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlDdlCommandHandlers), "DropTable")]
public sealed class DropTableDdlCommandHandler : MsSqlDdlCommandHandlerBase<DropTableCommand>
{
    /// <summary>Initializes a new instance of the <see cref="DropTableDdlCommandHandler"/> class.</summary>
    public DropTableDdlCommandHandler()
        : base("DropTable")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<string> Write(DropTableCommand command) =>
        GenericResult<string>.Success(MsSqlDdlGenerator.GenerateDropTable(command));

    /// <inheritdoc/>
    protected override IEnumerable<string?> NamesOf(DropTableCommand command) =>
        [.. MsSqlDdlGenerator.Stated(command.SchemaName), command.TableName];
}
