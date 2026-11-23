using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;

namespace Fdw.Schema.Ddl.MsSql.CommandHandlers;

/// <summary>Writes the SQL Server statement of a CreateIndex command.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlDdlCommandHandlers), "CreateIndex")]
public sealed class CreateIndexDdlCommandHandler : MsSqlDdlCommandHandlerBase<CreateIndexCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateIndexDdlCommandHandler"/> class.</summary>
    public CreateIndexDdlCommandHandler()
        : base("CreateIndex")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<string> Write(CreateIndexCommand command) =>
        GenericResult<string>.Success(MsSqlDdlGenerator.GenerateCreateIndex(command));

    /// <inheritdoc/>
    protected override IEnumerable<string?> NamesOf(CreateIndexCommand command) =>
        [.. MsSqlDdlGenerator.Stated(command.SchemaName), command.TableName, command.IndexName, .. command.Definition.Columns, .. (command.Definition.IncludeColumns ?? [])];
}
