using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Catalog;
using Fdw.Tds.Endpoints.Sql.Queries;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// SELECT: one data set table read through its store, or the catalog and expressions evaluated in memory.
/// </summary>
/// <remarks>
/// SELECT INTO creates a table and is refused as a write. A common table expression, UNION and the other
/// set operators are refused with error 40517 for now.
/// </remarks>
[TypeOption(typeof(SqlStatementHandlers), "Select", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SelectHandler : SqlStatementHandlerBase<SelectStatement>
{
    /// <summary>Initializes a new instance of the <see cref="SelectHandler"/> class.</summary>
    public SelectHandler()
        : base("Select")
    {
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<SqlStatementOutcome>> Run(SelectStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        if (statement.Into is not null)
            return SqlRefusal.Refuse<SqlStatementOutcome>(SqlServerErrors.ReadOnly(context.Session.Database, statement.StartLine), context.Logger);

        if (statement.WithCtesAndXmlNamespaces is not null || statement.QueryExpression is not QuerySpecification query)
            return SqlRefusal.Refuse<SqlStatementOutcome>(SqlServerErrors.NotSupported(SqlText.Of(statement), statement.StartLine), context.Logger);

        var scope = SqlScope.Of(context);
        var output = query.FromClause is { TableReferences: [NamedTableReference named] } && !IsCatalog(named)
            ? await DataTableSelect.Run(query, named, scope, cancellationToken).ConfigureAwait(false)
            : InMemorySelect.Run(query, scope);
        if (output.IsFailure)
            return output.ToNewResult<SqlStatementOutcome>();

        if (output.Value!.AssignedOnly)
            return GenericResult<SqlStatementOutcome>.Success(SqlStatementOutcome.Ran());

        var written = await ResultSetWriter.Write(context, output.Value, statement.StartLine, cancellationToken).ConfigureAwait(false);
        return written.IsFailure
            ? written.ToNewResult<SqlStatementOutcome>()
            : GenericResult<SqlStatementOutcome>.Success(SqlStatementOutcome.Selected(written.Value));
    }

    private static bool IsCatalog(NamedTableReference named)
        => named.SchemaObject.SchemaIdentifier?.Value is { } schema && CatalogViews.IsCatalogSchema(schema);
}
