using System.Linq;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Ddl.Definitions;

namespace Fdw.Schema.Endpoints.Ddl;

/// <summary>
/// The DDL command an execute-ddl request asks for (FDW-3617), in the commands of <c>Fdw.Schema.Ddl</c>, the one DDL model: the connection's
/// type writes it in its own dialect and runs it. Nothing here names an engine.
/// </summary>
public static class ExecuteDdlCommands
{
    /// <summary>
    /// Builds the CREATE TABLE the request asks for. A column the request marks as part of the key is NOT NULL, as a key column must be; the
    /// key is named for its table, as the generated DDL names it. A column with no name or no type is refused before this is called.
    /// </summary>
    /// <param name="request">The request, whose table and columns are non-empty.</param>
    /// <returns>The command.</returns>
    public static IDdlCommand CreateTable(ExecuteDdlRequest request)
    {
        var keyColumns = request.Columns.Where(column => column.IsPrimaryKey).Select(column => column.Name).ToList();

        return new CreateTableCommand
        {
            SchemaName = request.SchemaName,
            TableName = request.TableName,
            Columns = [.. request.Columns.Select(column => new DdlColumnDefinition
            {
                Name = column.Name,
                SqlType = column.SqlType,
                MaxLength = column.MaxLength,
                IsNullable = !(column.IsRequired || column.IsPrimaryKey),
                IsIdentity = column.IsIdentity,
            })],
            PrimaryKeyName = keyColumns.Count > 0 ? $"PK_{request.TableName}" : null,
            PrimaryKeyColumns = keyColumns.Count > 0 ? keyColumns : null,
        };
    }
}
