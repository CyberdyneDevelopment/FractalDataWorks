using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;

namespace Fdw.Schema.Ddl.MsSql.CommandHandlers;

/// <summary>Writes the SQL Server statement of a CreateTable command.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlDdlCommandHandlers), "CreateTable")]
public sealed class CreateTableDdlCommandHandler : MsSqlDdlCommandHandlerBase<CreateTableCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateTableDdlCommandHandler"/> class.</summary>
    public CreateTableDdlCommandHandler()
        : base("CreateTable")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<string> Write(CreateTableCommand command) =>
        MsSqlDdlGenerator.GenerateCreateTable(command);

    /// <inheritdoc/>
    protected override IEnumerable<string?> NamesOf(CreateTableCommand command) =>
        MsSqlDdlGenerator.TableNames(command);
}
