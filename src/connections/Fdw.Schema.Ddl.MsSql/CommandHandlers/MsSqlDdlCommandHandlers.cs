using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;

namespace Fdw.Schema.Ddl.MsSql.CommandHandlers;

/// <summary>
/// The DDL command handlers of the SQL Server dialect, found by the command's own type name (<c>command.CommandType.Name</c>); a command
/// type with no handler (AlterTable, InsertData, CreateView, DropView) gets <c>NotFound</c> and is refused as CommandGenerationFailed.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(MsSqlDdlCommandHandlerBase), typeof(IMsSqlDdlCommandHandler), typeof(MsSqlDdlCommandHandlers))]
public abstract partial class MsSqlDdlCommandHandlers : TypeCollectionBase<MsSqlDdlCommandHandlerBase, IMsSqlDdlCommandHandler>
{
}
