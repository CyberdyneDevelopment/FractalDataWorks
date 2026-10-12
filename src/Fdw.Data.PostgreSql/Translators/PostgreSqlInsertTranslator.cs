using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.PostgreSql.Results;
using Fdw.Data.PostgreSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Npgsql;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Translates InsertCommand to PostgreSQL INSERT statement.
/// </summary>
/// <remarks>
/// <para>
/// Builds PostgreSQL INSERT statements with:
/// <list type="bullet">
/// <item>INSERT INTO - container's physical name with double-quoted identifiers</item>
/// <item>Column list - from container schema (excludes identity columns)</item>
/// <item>VALUES clause - parameterized values from command data</item>
/// <item>RETURNING - returns generated identity/serial value (instead of SCOPE_IDENTITY)</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(PostgreSqlDataCommandTranslators), "Insert", RestrictToCurrentCompilation = true)]
public sealed class PostgreSqlInsertTranslator : PostgreSqlDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlInsertTranslator"/> class.
    /// </summary>
    public PostgreSqlInsertTranslator()
        : base("Insert")
    {
    }

    /// <summary>
    /// Translates an InsertCommand to a PostgreSQL INSERT statement.
    /// </summary>
    public override Task<IGenericResult<NpgsqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (container == null)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("ContainerNull")));
            }

            if (container.Path is not IDatabasePath dbPath)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(PostgreSqlDataResultCodes.ByName("InvalidContainerPath")));
            }

            var dataObj = GetCommandData(command);
            if (dataObj == null)
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(
                        PostgreSqlDataResultCodes.ByName("MissingInputData"),
                        ResultDetails.Create("CommandType", "InsertCommand")));
            }

            var npgsqlCommand = BuildInsertStatement(container, dbPath, dataObj);

            return Task.FromResult(GenericResult<NpgsqlCommand>.Success(npgsqlCommand));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<NpgsqlCommand>.Failure(
                    PostgreSqlDataResultCodes.ByName("InsertTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Builds a complete PostgreSQL INSERT statement with RETURNING clause.
    /// </summary>
    private static NpgsqlCommand BuildInsertStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        object data)
    {
        var dialect = dbPath.Dialect;

        // Get columns from schema (exclude identity and computed columns)
        var fields = container.Schema.Fields
            .Where(f => !f.IsIdentity && !f.IsComputed)
            .ToList();

        if (fields.Count == 0)
        {
            throw new InvalidOperationException($"Container {container.Name} has no insertable fields");
        }

        var allFieldNames = fields.Select(f => f.Name).ToList();

        // Only include fields that have matching properties on the data object
        var dataType = data.GetType();
        var fieldNames = allFieldNames.Where(f => dataType.GetProperty(f) != null).ToList();

        if (fieldNames.Count == 0)
        {
            throw new InvalidOperationException(
                $"Data object has no properties matching insertable fields for container {container.Name}");
        }

        // Build column list with dialect-quoted identifiers
        var columnList = string.Join(", ", fieldNames.Select(f => dialect.QuoteIdentifier(f)));

        // Build parameter list
        var p = dialect.ParameterPrefix;
        var paramList = string.Join(", ", fieldNames.Select(f => $"{p}{f}"));

        // Build INSERT statement with RETURNING clause for generated identity
        var identityField = container.Schema.Fields.FirstOrDefault(f => f.IsIdentity);
        var returningClause = identityField != null
            ? $" RETURNING {dialect.QuoteIdentifier(identityField.Name)}"
            : string.Empty;

        var sql = $"INSERT INTO {BuildQualifiedTableName(dbPath)} ({columnList}) VALUES ({paramList}){returningClause};";

        var command = CreateCommand(sql);
        AddParametersFromObject(data, fieldNames, (n, v) => AddParameter(command, n, v));

        return command;
    }
}
