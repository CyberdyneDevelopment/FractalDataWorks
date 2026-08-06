using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Data.MsSql.Results;
using Fdw.Data.MsSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Translates FindCommand to T-SQL SELECT with LIKE-based cross-field search.
/// </summary>
/// <remarks>
/// <para>
/// Builds T-SQL SELECT statements with:
/// <list type="bullet">
/// <item>SELECT * FROM [schema].[table]</item>
/// <item>WHERE col1 LIKE @p0 OR col2 LIKE @p0 ...</item>
/// <item>Optional COLLATE for case sensitivity control</item>
/// <item>Optional TOP N for max results</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "Find", RestrictToCurrentCompilation = true)]
public sealed class MsSqlFindTranslator : MsSqlDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlFindTranslator"/> class.
    /// </summary>
    public MsSqlFindTranslator()
        : base("Find")
    {
    }

    /// <summary>
    /// Base Translate - dispatches to find-specific logic or returns error for invalid command types.
    /// </summary>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        if (command is FindCommand<object> ||
            command.GetType().IsGenericType &&
            command.GetType().GetGenericTypeDefinition() == typeof(FindCommand<>))
        {
            return TranslateFind(command, container);
        }

        return Task.FromResult(
            GenericResult<SqlCommand>.Failure(
                MsSqlTranslatorLog.InvalidCommandType(
                    NullLogger<MsSqlFindTranslator>.Instance,
                    "MsSqlFindTranslator",
                    "FindCommand<T>",
                    command.GetType().Name)));
    }

    private static Task<IGenericResult<SqlCommand>> TranslateFind(
        IDataCommand command,
        IStorageContainer container)
    {
        try
        {
            if (container == null)
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("ContainerNull")));
            }

            if (container.Path is not IDatabasePath dbPath)
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath")));
            }

            // Extract FindCommand properties via reflection (generic type erasure)
            var commandType = command.GetType();
            var searchTerm = (string)(commandType.GetProperty("SearchTerm")!.GetValue(command) ?? string.Empty);
            var fieldNames = commandType.GetProperty("FieldNames")!.GetValue(command) as IReadOnlyList<string>;
            var caseSensitive = (bool)commandType.GetProperty("CaseSensitive")!.GetValue(command)!;
            var maxResults = commandType.GetProperty("MaxResults")!.GetValue(command) as int?;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(
                        MsSqlDataResultCodes.ByName("FindTranslationFailed"),
                        ResultDetails.Create("ErrorMessage", "SearchTerm cannot be empty")));
            }

            var sqlCommand = BuildFindStatement(
                container,
                dbPath,
                searchTerm,
                fieldNames,
                caseSensitive,
                maxResults);

            return Task.FromResult(GenericResult<SqlCommand>.Success(sqlCommand));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<SqlCommand>.Failure(
                    MsSqlDataResultCodes.ByName("FindTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static SqlCommand BuildFindStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        string searchTerm,
        IReadOnlyList<string>? fieldNames,
        bool caseSensitive,
        int? maxResults)
    {
        var dialect = dbPath.Dialect;
        var sql = new StringBuilder();

        // SELECT clause
        if (maxResults.HasValue)
        {
            sql.Append(CultureInfo.InvariantCulture, $"SELECT TOP ({maxResults.Value})");
        }
        else
        {
            sql.Append("SELECT");
        }

        // Project all fields from container schema or SELECT *
        if (container.Schema.Fields.Count > 0)
        {
            var fields = string.Join(", ", container.Schema.Fields.Select(f => dialect.QuoteIdentifier(f.Name)));
            sql.Append(CultureInfo.InvariantCulture, $" {fields}");
        }
        else
        {
            sql.Append(" *");
        }

        // FROM clause
        sql.Append(CultureInfo.InvariantCulture, $" FROM {BuildQualifiedTableName(dbPath)}");

        // Determine which columns to search
        var searchColumns = ResolveSearchColumns(container, fieldNames);

        if (searchColumns.Count == 0)
        {
            // No searchable columns - return query that returns no rows
            sql.Append(CultureInfo.InvariantCulture, $" WHERE {dialect.AlwaysFalsePredicate}");
            return CreateCommand(sql.ToString());
        }

        // Build WHERE clause with LIKE conditions
        var command = CreateCommand(sql.ToString());

        var collateClause = caseSensitive
            ? " COLLATE Latin1_General_CS_AS"
            : string.Empty;

        var likeConditions = new List<string>();
        foreach (var column in searchColumns)
        {
            if (!IsValidColumnName(column))
            {
                throw new ArgumentException(
                    $"Invalid column name '{column}'. Only alphanumeric characters and underscores allowed.",
                    nameof(fieldNames));
            }

            likeConditions.Add($"{dialect.QuoteIdentifier(column)}{collateClause} LIKE {dialect.ParameterPrefix}searchTerm");
        }

        command.CommandText += $" WHERE {string.Join(" OR ", likeConditions)}";

        // Add the search term parameter with wildcards
        AddParameter(command, "searchTerm", $"%{EscapeLikeWildcards(searchTerm)}%");

        return command;
    }

    private static IReadOnlyList<string> ResolveSearchColumns(
        IStorageContainer container,
        IReadOnlyList<string>? fieldNames)
    {
        if (fieldNames != null && fieldNames.Count > 0)
        {
            return fieldNames;
        }

        // Default: search all string-type columns
        var stringColumns = new List<string>();
        foreach (var field in container.Schema.Fields)
        {
            if (field.FieldType.ClrType == typeof(string))
            {
                stringColumns.Add(field.Name);
            }
        }

        return stringColumns;
    }

    private static string EscapeLikeWildcards(string value)
    {
        return value
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
    }
}
