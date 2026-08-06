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
using Fdw.Data.PostgreSql.Logging;
using Fdw.Data.PostgreSql.Results;
using Fdw.Data.PostgreSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Translates FindCommand to PostgreSQL SELECT with LIKE/ILIKE-based cross-field search.
/// </summary>
/// <remarks>
/// <para>
/// Builds PostgreSQL SELECT statements with:
/// <list type="bullet">
/// <item>SELECT * FROM "schema"."table"</item>
/// <item>WHERE col1 ILIKE @searchTerm OR col2 ILIKE @searchTerm ... (case-insensitive by default)</item>
/// <item>WHERE col1 LIKE @searchTerm OR col2 LIKE @searchTerm ... (case-sensitive)</item>
/// <item>Optional LIMIT N for max results</item>
/// </list>
/// </para>
/// <para>
/// Uses PostgreSQL ILIKE for case-insensitive search instead of COLLATE.
/// </para>
/// </remarks>
[TypeOption(typeof(PostgreSqlDataCommandTranslators), "Find", RestrictToCurrentCompilation = true)]
public sealed class PostgreSqlFindTranslator : PostgreSqlDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlFindTranslator"/> class.
    /// </summary>
    public PostgreSqlFindTranslator()
        : base("Find")
    {
    }

    /// <summary>
    /// Base Translate - dispatches to find-specific logic or returns error for invalid command types.
    /// </summary>
    public override Task<IGenericResult<NpgsqlCommand>> Translate(
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
            GenericResult<NpgsqlCommand>.Failure(
                PostgreSqlTranslatorLog.InvalidCommandType(
                    NullLogger<PostgreSqlFindTranslator>.Instance,
                    "PostgreSqlFindTranslator",
                    "FindCommand<T>",
                    command.GetType().Name)));
    }

    private static Task<IGenericResult<NpgsqlCommand>> TranslateFind(
        IDataCommand command,
        IStorageContainer container)
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

            // Extract FindCommand properties via reflection (generic type erasure)
            var commandType = command.GetType();
            var searchTerm = (string)(commandType.GetProperty("SearchTerm")!.GetValue(command) ?? string.Empty);
            var fieldNames = commandType.GetProperty("FieldNames")!.GetValue(command) as IReadOnlyList<string>;
            var caseSensitive = (bool)commandType.GetProperty("CaseSensitive")!.GetValue(command)!;
            var maxResults = commandType.GetProperty("MaxResults")!.GetValue(command) as int?;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Task.FromResult(
                    GenericResult<NpgsqlCommand>.Failure(
                        PostgreSqlDataResultCodes.ByName("FindTranslationFailed"),
                        ResultDetails.Create("ErrorMessage", "SearchTerm cannot be empty")));
            }

            var npgsqlCommand = BuildFindStatement(
                container,
                dbPath,
                searchTerm,
                fieldNames,
                caseSensitive,
                maxResults);

            return Task.FromResult(GenericResult<NpgsqlCommand>.Success(npgsqlCommand));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<NpgsqlCommand>.Failure(
                    PostgreSqlDataResultCodes.ByName("FindTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static NpgsqlCommand BuildFindStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        string searchTerm,
        IReadOnlyList<string>? fieldNames,
        bool caseSensitive,
        int? maxResults)
    {
        var dialect = dbPath.Dialect;
        var sql = new StringBuilder();

        sql.Append("SELECT");

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
            sql.Append(CultureInfo.InvariantCulture, $" WHERE {dialect.AlwaysFalsePredicate}");
            return CreateCommand(sql.ToString());
        }

        // Build WHERE clause with LIKE/ILIKE conditions
        var command = CreateCommand(sql.ToString());

        // PostgreSQL: ILIKE for case-insensitive, LIKE for case-sensitive
        var likeOperator = caseSensitive ? "LIKE" : "ILIKE";

        var likeConditions = new List<string>();
        foreach (var column in searchColumns)
        {
            if (!IsValidColumnName(column))
            {
                throw new ArgumentException(
                    $"Invalid column name '{column}'. Only alphanumeric characters and underscores allowed.",
                    nameof(fieldNames));
            }

            likeConditions.Add($"{dialect.QuoteIdentifier(column)} {likeOperator} {dialect.ParameterPrefix}searchTerm");
        }

        command.CommandText += $" WHERE {string.Join(" OR ", likeConditions)}";

        // Add the search term parameter with wildcards
        AddParameter(command, "searchTerm", $"%{EscapeLikeWildcards(searchTerm)}%");

        // LIMIT instead of TOP (PostgreSQL syntax)
        if (maxResults.HasValue)
        {
            command.CommandText += $" LIMIT {maxResults.Value}";
        }

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
        // PostgreSQL LIKE escape: backslash is the default escape character
        return value
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_");
    }
}
