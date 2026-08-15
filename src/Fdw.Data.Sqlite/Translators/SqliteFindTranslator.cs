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
using Fdw.Data.Sqlite.Logging;
using Fdw.Data.Sqlite.Results;
using Fdw.Data.Sqlite.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.Sqlite;

namespace Fdw.Services.Connections.Sqlite;

/// <summary>
/// Translates FindCommand to a SQLite SELECT with LIKE-based cross-field search.
/// </summary>
[TypeOption(typeof(SqliteDataCommandTranslators), "Find", RestrictToCurrentCompilation = true)]
public sealed class SqliteFindTranslator : SqliteDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteFindTranslator"/> class.
    /// </summary>
    public SqliteFindTranslator()
        : base("Find")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<SqliteCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        if (command is FindCommand<object> ||
            (command.GetType().IsGenericType &&
             command.GetType().GetGenericTypeDefinition() == typeof(FindCommand<>)))
        {
            return TranslateFind(command, container);
        }

        return Task.FromResult(GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidCommandType")));
    }

    private static Task<IGenericResult<SqliteCommand>> TranslateFind(
        IDataCommand command,
        IStorageContainer container)
    {
        try
        {
            if (container == null)
                return Task.FromResult(GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("ContainerNull")));

            if (container.Path is not IDatabasePath dbPath)
                return Task.FromResult(GenericResult<SqliteCommand>.Failure(SqliteDataResultCodes.ByName("InvalidContainerPath")));

            var commandType = command.GetType();
            var searchTerm = (string)(commandType.GetProperty("SearchTerm")!.GetValue(command) ?? string.Empty);
            var fieldNames = commandType.GetProperty("FieldNames")!.GetValue(command) as IReadOnlyList<string>;
            var caseSensitive = (bool)commandType.GetProperty("CaseSensitive")!.GetValue(command)!;
            var maxResults = commandType.GetProperty("MaxResults")!.GetValue(command) as int?;

            if (string.IsNullOrWhiteSpace(searchTerm))
                return Task.FromResult(
                    GenericResult<SqliteCommand>.Failure(
                        SqliteDataResultCodes.ByName("FindTranslationFailed"),
                        ResultDetails.Create("ErrorMessage", "SearchTerm cannot be empty")));

            return Task.FromResult(GenericResult<SqliteCommand>.Success(
                BuildFindStatement(container, dbPath, searchTerm, fieldNames, caseSensitive, maxResults)));
        }
        catch (Exception ex)
        {
            SqliteConnectionLog.TranslationFailed(TranslatorLogger, ex, "Find", ex.Message);
            return Task.FromResult(
                GenericResult<SqliteCommand>.Failure(
                    SqliteDataResultCodes.ByName("FindTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static SqliteCommand BuildFindStatement(
        IStorageContainer container,
        IDatabasePath dbPath,
        string searchTerm,
        IReadOnlyList<string>? fieldNames,
        bool caseSensitive,
        int? maxResults)
    {
        var dialect = dbPath.Dialect;
        var sql = new StringBuilder("SELECT");

        if (container.Schema.GetProjectableFields().Count > 0)
        {
            var fields = string.Join(", ", container.Schema.GetProjectableFields().Select(f => dialect.QuoteIdentifier(f.Name)));
            sql.Append(CultureInfo.InvariantCulture, $" {fields}");
        }
        else
        {
            sql.Append(" *");
        }

        sql.Append(CultureInfo.InvariantCulture, $" FROM {BuildQualifiedTableName(dbPath)}");

        var searchColumns = fieldNames != null && fieldNames.Count > 0
            ? (IReadOnlyList<string>)fieldNames
            : container.Schema.Fields
                .Where(f => f.FieldType.ClrType == typeof(string))
                .Select(f => f.Name)
                .ToList();

        if (searchColumns.Count == 0)
        {
            sql.Append(CultureInfo.InvariantCulture, $" WHERE {dialect.AlwaysFalsePredicate}");
            return CreateCommand(sql.ToString());
        }

        var command = CreateCommand(sql.ToString());

        // Why: SQLite LIKE is case-insensitive by default for ASCII only. Use GLOB (case-sensitive)
        // or LIKE with no COLLATE modifier (case-insensitive). For the case-sensitive path, escape
        // with GLOB wildcards instead of LIKE wildcards.
        var likeConditions = new List<string>();
        foreach (var column in searchColumns)
        {
            if (!IsValidColumnName(column))
                throw new ArgumentException($"Invalid column name '{column}'.", nameof(fieldNames));

            if (caseSensitive)
                likeConditions.Add($"{dialect.QuoteIdentifier(column)} GLOB {dialect.ParameterPrefix}searchTerm");
            else
                likeConditions.Add($"{dialect.QuoteIdentifier(column)} LIKE {dialect.ParameterPrefix}searchTerm");
        }

        command.CommandText += $" WHERE {string.Join(" OR ", likeConditions)}";

        var searchValue = caseSensitive
            ? $"*{EscapeGlobWildcards(searchTerm)}*"
            : $"%{EscapeLikeWildcards(searchTerm)}%";

        AddParameter(command, "searchTerm", searchValue);

        if (maxResults.HasValue)
            command.CommandText += $" LIMIT {maxResults.Value}";

        return command;
    }

    private static string EscapeLikeWildcards(string value)
        => value.Replace("%", "\\%").Replace("_", "\\_");

    // Why: escape the literal-bracket escape char `[` FIRST. Escaping "*"/"?" first injects
    // `[` characters that the trailing `[`→`[[]` Replace would then re-escape, corrupting any
    // term that legitimately contains `*`, `?`, or `[`.
    private static string EscapeGlobWildcards(string value)
        => value.Replace("[", "[[]").Replace("*", "[*]").Replace("?", "[?]");
}
