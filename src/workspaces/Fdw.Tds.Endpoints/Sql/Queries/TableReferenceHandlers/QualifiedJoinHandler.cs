using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Rows;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>Reads a join with an ON condition.</summary>
[TypeOption(typeof(TableReferenceHandlers), "QualifiedJoin")]
[ExcludeFromCodeCoverage]
public sealed class QualifiedJoinHandler : TableReferenceHandlerBase<QualifiedJoin>
{
    /// <summary>Initializes a new instance of the <see cref="QualifiedJoinHandler"/> class.</summary>
    public QualifiedJoinHandler()
        : base("QualifiedJoin")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<SqlRowSet> Handle(QualifiedJoin reference, SqlScope outer)
        => InMemorySelect.Qualified(reference, outer);
}
