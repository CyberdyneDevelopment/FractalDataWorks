using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Rows;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>Reads a CROSS JOIN; any other join written without a condition stays unsupported.</summary>
[TypeOption(typeof(TableReferenceHandlers), "UnqualifiedJoin")]
[ExcludeFromCodeCoverage]
public sealed class UnqualifiedJoinHandler : TableReferenceHandlerBase<UnqualifiedJoin>
{
    /// <summary>Initializes a new instance of the <see cref="UnqualifiedJoinHandler"/> class.</summary>
    public UnqualifiedJoinHandler()
        : base("UnqualifiedJoin")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<SqlRowSet> Handle(UnqualifiedJoin reference, SqlScope outer)
        => reference.UnqualifiedJoinType == UnqualifiedJoinType.CrossJoin
            ? InMemorySelect.Cross(reference, outer)
            : InMemorySelect.Unsupported(reference, outer);
}
