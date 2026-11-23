using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Sql;

// What every SQL translator shares about a query that stacks containers' rows (FDW-6946): one UNION ALL statement, written the same way by
// every SQL kind, the filter, order and page applied to the stacked rows by the branches' aliases.
public abstract partial class SqlDataCommandTranslatorBase<TCommand>
{
    /// <summary>
    /// Writes a query that stacks rows (FDW-6946): <c>SELECT</c> the aliases <c>FROM (</c> the query's own projection over its own container
    /// <c>UNION ALL</c> each branch <c>) AS u</c>, then the filter, order and page on the aliases.
    /// </summary>
    /// <param name="query">A query whose <see cref="IQueryCommand.Unions"/> is not empty.</param>
    /// <param name="container">The query's own container: the first branch.</param>
    /// <param name="dialect">The SQL dialect for quoting and the paging clause.</param>
    /// <param name="addParam">Adds a filter parameter to the command, by its name without the prefix.</param>
    /// <returns>
    /// The statement, or the refusal: <c>UnionWithJoinsOrGrouping</c>, <c>UnionIdentifierInvalid</c> for a name that is not a plain one,
    /// <c>UnionContainerNotDeclaredOnce</c>, <c>UnionBranchesDoNotFit</c> for a branch whose aliases are not the first branch's, in order, and
    /// <c>UnionPagedWithoutOrder</c>.
    /// </returns>
    /// <remarks>
    /// Every identifier is checked before any is written (SQL injection prevention); every value is a parameter. Rows are kept whole, none
    /// removed as a duplicate. A filter on the stacked rows is the same as the filter on each branch, and the order and page can only be the
    /// stacked rows', so all three are written once, outside.
    /// </remarks>
    protected static (IGenericMessage? Refusal, string Sql) BuildUnionStatement(
        IQueryCommand query, IDataContainer container, ISqlDialect dialect, Action<string, object?> addParam)
    {
        if (query.Joins.Count > 0 || query.Aggregation is not null)
            return (QueryCommandLog.UnionWithJoinsOrGrouping(NullLogger.Instance, container.Name, query.Joins.Count, query.Aggregation is not null), string.Empty);

        IReadOnlyList<ProjectionField> first = query.Projection?.Fields is { } projected ? projected : [];
        var branches = new List<(string Container, IReadOnlyList<ProjectionField> Fields)> { (container.Name, first) };
        branches.AddRange(query.Unions.Select(branch => (branch.ContainerName, branch.Fields)));

        if (UnionRefusal(branches, container) is { } refusal)
            return (refusal, string.Empty);

        if (query.Paging is not null && query.Ordering?.OrderedFields is not { Count: > 0 })
            return (QueryCommandLog.UnionPagedWithoutOrder(NullLogger.Instance, container.Name), string.Empty);

        return (null, StackedSql(query, container, dialect, addParam, branches));
    }

    // The statement of branches UnionRefusal accepted: the first branch's aliases selected from the stacked branches, then the filter, order and
    // page on those aliases (FDW-6946; split out of BuildUnionStatement for FDW007, r220).
    private static string StackedSql(
        IQueryCommand query,
        IDataContainer container,
        ISqlDialect dialect,
        Action<string, object?> addParam,
        List<(string Container, IReadOnlyList<ProjectionField> Fields)> branches)
    {
        var sql = new StringBuilder("SELECT ");
        sql.Append(string.Join(", ", branches[0].Fields.Select(field => dialect.QuoteIdentifier(field.Alias!))));
        sql.Append(" FROM (");
        for (var index = 0; index < branches.Count; index++)
        {
            if (index > 0)
                sql.Append(" UNION ALL ");
            sql.Append("SELECT ");
            sql.Append(string.Join(", ", branches[index].Fields.Select(field =>
                $"{dialect.QuoteIdentifier(field.PropertyName)} AS {dialect.QuoteIdentifier(field.Alias!)}")));
            sql.Append(" FROM ");
            sql.Append(UnionTable(container, branches[index].Container, dialect));
        }

        sql.Append(") AS ");
        sql.Append(dialect.QuoteIdentifier("u"));

        if (query.Filter?.Root is not null)
            sql.Append(" WHERE ").Append(BuildWhereClause(query.Filter, dialect, addParam));

        if (query.Ordering?.OrderedFields is { Count: > 0 })
        {
            sql.Append(" ORDER BY ").Append(BuildOrderByClause(query.Ordering, dialect));
            if (query.Paging is not null)
                sql.Append(' ').Append(dialect.BuildPagingClause(query.Paging));
        }

        return sql.ToString();
    }

    // The first refusal of the branches' names or columns, or null: every name a plain one, every container declared once in the query
    // container's store, and every branch giving the first branch's aliases in its order.
    private static IGenericMessage? UnionRefusal(List<(string Container, IReadOnlyList<ProjectionField> Fields)> branches, IDataContainer container)
    {
        var expected = string.Join(", ", branches[0].Fields.Select(field => field.Alias));
        foreach (var (name, fields) in branches)
        {
            var invalid = !IsValidColumnName(name) ? name
                : fields.SelectMany(field => new[] { field.PropertyName, field.Alias ?? string.Empty }).FirstOrDefault(identifier => !IsValidColumnName(identifier));
            if (invalid is not null)
                return QueryCommandLog.UnionIdentifierInvalid(NullLogger.Instance, container.Name, invalid);

            var declared = UnionContainers(container, name).Count;
            if (declared != 1)
                return QueryCommandLog.UnionContainerNotDeclaredOnce(NullLogger.Instance, container.Name, name, declared);

            var columns = string.Join(", ", fields.Select(field => field.Alias));
            if (fields.Count == 0 || !string.Equals(columns, expected, StringComparison.Ordinal))
                return QueryCommandLog.UnionBranchesDoNotFit(NullLogger.Instance, container.Name, name, columns, expected);
        }

        return null;
    }

    // The containers of the query container's store that carry this name; names are unique within a store.
    private static List<IDataContainer> UnionContainers(IDataContainer container, string name) =>
        [.. container.Parent.Store.Paths
            .SelectMany(path => path.Containers)
            .Where(candidate => string.Equals(candidate.Name, name, StringComparison.Ordinal))];

    // A branch's table, qualified as its own path states it: database and schema where the dialect has them, else the bare name.
    private static string UnionTable(IDataContainer container, string name, ISqlDialect dialect)
    {
        var quoted = dialect.QuoteIdentifier(name);
        if (UnionContainers(container, name)[0].Path is not IDatabasePath path || !dialect.SupportsSchemaNamespace || string.IsNullOrEmpty(path.Schema))
            return quoted;

        var schemaQualified = $"{dialect.QuoteIdentifier(path.Schema)}.{quoted}";
        return string.IsNullOrEmpty(path.Database) ? schemaQualified : $"{dialect.QuoteIdentifier(path.Database)}.{schemaQualified}";
    }
}
