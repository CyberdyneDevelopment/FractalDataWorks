using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Loads;
using Fdw.Commands.Data.ValueSources;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Data.MsSql.Results;
using Fdw.Data.MsSql.ValueSources;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Translates <see cref="IInsertFromQueryCommand"/> to one set-based <c>INSERT ... SELECT</c>. The source is a table
/// or view on the same server (it may be in another database there); the rows never leave SQL Server.
/// </summary>
/// <remarks>
/// Every name is validated and quoted; every value is a parameter. A target column can take its value from a source
/// column or from a value source (constant, current timestamp, next sequence value in the order of listed columns).
/// </remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "InsertFromQuery", RestrictToCurrentCompilation = true)]
public sealed class MsSqlInsertFromQueryTranslator : MsSqlDataCommandTranslatorBase
{
    private const string SourceAlias = "src";

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlInsertFromQueryTranslator"/> class.
    /// </summary>
    public MsSqlInsertFromQueryTranslator()
        : base("InsertFromQuery")
    {
    }

    /// <summary>
    /// Translates an insert-from-query command into an INSERT ... SELECT. Returns a failure result with a reason when
    /// the command cannot be translated.
    /// </summary>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<MsSqlInsertFromQueryTranslator>.Instance;
        var containerName = container is null ? "<null>" : container.Name;
        MsSqlInsertFromQueryTranslatorLog.Translating(logger, containerName);

        try
        {
            return Task.FromResult(TranslateCommand(command, container, containerName));
        }
        catch (Exception ex)
        {
            MsSqlInsertFromQueryTranslatorLog.TranslationFailed(logger, ex, containerName, ex.Message);
            return Task.FromResult(GenericResult<SqlCommand>.Failure(
                MsSqlDataResultCodes.ByName("InsertFromQueryTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static IGenericResult<SqlCommand> TranslateCommand(IDataCommand command, IStorageContainer? container, string containerName)
    {
        var logger = NullLogger<MsSqlInsertFromQueryTranslator>.Instance;
        if (container is null)
        {
            MsSqlInsertFromQueryTranslatorLog.ContainerNull(logger);
            return GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("ContainerNull"));
        }

        if (container.Path is not IDatabasePath path)
        {
            MsSqlInsertFromQueryTranslatorLog.InvalidContainerPath(logger, containerName);
            return GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath"));
        }

        if (command is not IInsertFromQueryCommand insert)
        {
            return GenericResult<SqlCommand>.Failure(
                MsSqlTranslatorLog.InvalidCommandType(logger, "MsSqlInsertFromQueryTranslator", nameof(IInsertFromQueryCommand), command.GetType().Name));
        }

        var problem = Problem(insert.Options, container);
        if (problem is not null)
            return Refused(containerName, problem);

        var sql = Statement(insert.Options, container, path);
        if (sql.Problem is not null)
            return Refused(containerName, sql.Problem);

        MsSqlInsertFromQueryTranslatorLog.Translated(logger, containerName, insert.Options.TargetColumns.Count);
        return GenericResult<SqlCommand>.Success(sql.Command);
    }

    private static string? Problem(InsertFromQueryOptions options, IStorageContainer container)
    {
        var general = options.FirstProblem();
        if (general is not null)
            return general;

        var targets = MsSqlLoadColumns.UnknownOrUnsafe(container, options.TargetColumns);
        if (targets is not null)
            return targets;

        var bad = options.TargetColumns
            .FirstOrDefault(c => MsSqlLoadColumns.IsIdentity(container, c) || MsSqlLoadColumns.IsComputed(container, c));
        if (bad is not null)
            return $"target column '{bad}' is an identity or computed column, which the database fills";

        return SourceNameProblem(options);
    }

    private static string? SourceNameProblem(InsertFromQueryOptions options)
    {
        var source = options.Source;
        var names = new List<string> { source.Table };
        if (source.Schema is not null)
            names.Add(source.Schema);
        if (source.Database is not null)
            names.Add(source.Database);
        names.AddRange(options.WriteColumns);
        if (options.ValueSources is not null)
            names.AddRange(options.ValueSources.Select(v => v.Source).OfType<ColumnReferenceValueSource>().Select(c => c.SourceField));
        if (source.OrderBy is not null)
            names.AddRange(source.OrderBy);

        var unsafeName = MsSqlIdentifiers.FirstUnsafe(names);
        return unsafeName is null ? null : $"'{unsafeName}' is not a safe name (letters, digits and underscores only)";
    }

    private static (SqlCommand Command, string? Problem) Statement(InsertFromQueryOptions options, IStorageContainer container, IDatabasePath path)
    {
        var dialect = path.Dialect;
        var command = CreateCommand(string.Empty);
        var context = new MsSqlValueSourceContext(command, dialect, SourceAlias);
        var alias = dialect.QuoteIdentifier(SourceAlias);

        var expressions = options.WriteColumns.Select(c => $"{alias}.{dialect.QuoteIdentifier(c)}").ToList();
        var bindings = new List<ColumnValueBinding>();
        if (options.ValueSources is not null)
            bindings.AddRange(options.ValueSources);
        foreach (var binding in bindings)
        {
            var expression = MsSqlValueSourceRenderers.ByName(binding.Source.Kind).Render(binding.Source, context);
            if (expression is null)
                return (command, $"the value source for column '{binding.Column}' cannot be rendered");
            expressions.Add(expression);
        }

        var source = MsSqlIdentifiers.Qualified(dialect, options.Source.Database, options.Source.Schema, options.Source.Table);
        var targets = string.Join(", ", options.TargetColumns.Select(dialect.QuoteIdentifier));
        var sql = $"INSERT INTO {BuildQualifiedTableName(path)} ({targets}) SELECT {string.Join(", ", expressions)} FROM {source} AS {alias}";
        if (options.Source.Filter?.Root is not null)
            sql += $" WHERE {BuildWhereClause(options.Source.Filter, dialect, (n, v) => AddParameter(command, n, v), SourceAlias)}";
        if (options.Source.OrderBy is { Count: > 0 })
            sql += $" ORDER BY {string.Join(", ", options.Source.OrderBy.Select(o => $"{alias}.{dialect.QuoteIdentifier(o)}"))}";

        command.CommandText = sql;
        return (command, null);
    }

    private static IGenericResult<SqlCommand> Refused(string containerName, string reason)
    {
        MsSqlInsertFromQueryTranslatorLog.InvalidOptions(NullLogger<MsSqlInsertFromQueryTranslator>.Instance, containerName, reason);
        return Failed("InvalidInsertFromQueryOptions", "Reason", reason);
    }

    private static IGenericResult<SqlCommand> Failed(string code, string detailName, string detail) =>
        GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName(code), ResultDetails.Create(detailName, detail));
}
