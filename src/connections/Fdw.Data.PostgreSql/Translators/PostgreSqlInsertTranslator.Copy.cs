using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.PostgreSql.Results;
using Fdw.Results;
using Fdw.Services.Connections.Sql;

namespace Fdw.Data.PostgreSql;

public sealed partial class PostgreSqlInsertTranslator
{
    /// <summary>
    /// Plans the binary COPY of an insert's rows: the same rows and the same columns the serial statements would write.
    /// </summary>
    /// <param name="command">The insert command.</param>
    /// <param name="container">The container the rows go into.</param>
    /// <returns>The copy plan, or the coded failure that says why the rows cannot be copied.</returns>
    public static IGenericResult<PostgreSqlBulkCopyPlan> Plan(IDataCommand command, IStorageContainer container)
    {
        try
        {
            var source = Source(command, container);
            if (source.IsFailure)
                return source.ToNewResult<PostgreSqlBulkCopyPlan>();

            var (dbPath, collection) = source.Value!;
            var dialect = dbPath.Dialect;
            var insertable = InsertableFields(container);
            if (insertable.Count == 0)
                throw new InvalidOperationException($"Container {container.Name} has no insertable fields");

            var rows = collection.Cast<object>().ToList();
            var fields = InsertColumns.Supplied(insertable, rows);
            if (fields.Count == 0)
            {
                throw new InvalidOperationException(
                    $"The rows have no properties matching insertable fields for container {container.Name}");
            }

            var columnList = string.Join(", ", fields.Select(f => dialect.QuoteIdentifier(f.Name)));
            var copySql = $"COPY {BuildQualifiedTableName(dbPath)} ({columnList}) FROM STDIN BINARY";

            return GenericResult<PostgreSqlBulkCopyPlan>.Success(
                new PostgreSqlBulkCopyPlan(copySql, [.. fields.Select(f => f.Name)], rows));
        }
        catch (Exception ex)
        {
            return GenericResult<PostgreSqlBulkCopyPlan>.Failure(
                PostgreSqlDataResultCodes.ByName("InsertTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message));
        }
    }
}
