using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Rows;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>Reads a parenthesized join.</summary>
[TypeOption(typeof(TableReferenceHandlers), "JoinParenthesisTableReference")]
[ExcludeFromCodeCoverage]
public sealed class JoinParenthesisTableReferenceHandler : TableReferenceHandlerBase<JoinParenthesisTableReference>
{
    /// <summary>Initializes a new instance of the <see cref="JoinParenthesisTableReferenceHandler"/> class.</summary>
    public JoinParenthesisTableReferenceHandler()
        : base("JoinParenthesisTableReference")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<SqlRowSet> Handle(JoinParenthesisTableReference reference, SqlScope outer)
        => InMemorySelect.Reference(reference.Join, outer);
}
