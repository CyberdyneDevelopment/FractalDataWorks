using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;

namespace Fdw.Schema.Ddl.MsSql.CommandHandlers;

/// <summary>Writes the SQL Server statement of a DropSchema command.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlDdlCommandHandlers), "DropSchema")]
public sealed class DropSchemaDdlCommandHandler : MsSqlDdlCommandHandlerBase<DropSchemaCommand>
{
    /// <summary>Initializes a new instance of the <see cref="DropSchemaDdlCommandHandler"/> class.</summary>
    public DropSchemaDdlCommandHandler()
        : base("DropSchema")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<string> Write(DropSchemaCommand command) =>
        GenericResult<string>.Success(MsSqlDdlGenerator.GenerateDropSchema(command));

    /// <inheritdoc/>
    protected override IEnumerable<string?> NamesOf(DropSchemaCommand command) =>
        [command.Name];
}
