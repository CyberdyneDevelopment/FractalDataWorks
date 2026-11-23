using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;

namespace Fdw.Schema.Ddl.MsSql.CommandHandlers;

/// <summary>Writes the SQL Server statement of a CreateSchema command.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlDdlCommandHandlers), "CreateSchema")]
public sealed class CreateSchemaDdlCommandHandler : MsSqlDdlCommandHandlerBase<CreateSchemaCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateSchemaDdlCommandHandler"/> class.</summary>
    public CreateSchemaDdlCommandHandler()
        : base("CreateSchema")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<string> Write(CreateSchemaCommand command) =>
        GenericResult<string>.Success(MsSqlDdlGenerator.GenerateCreateSchema(command));

    /// <inheritdoc/>
    protected override IEnumerable<string?> NamesOf(CreateSchemaCommand command) =>
        [command.Name];
}
