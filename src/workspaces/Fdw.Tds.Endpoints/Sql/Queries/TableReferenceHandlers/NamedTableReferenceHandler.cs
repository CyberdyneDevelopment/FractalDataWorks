using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Rows;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>Reads a named table or view.</summary>
[TypeOption(typeof(TableReferenceHandlers), "NamedTableReference")]
[ExcludeFromCodeCoverage]
public sealed class NamedTableReferenceHandler : TableReferenceHandlerBase<NamedTableReference>
{
    /// <summary>Initializes a new instance of the <see cref="NamedTableReferenceHandler"/> class.</summary>
    public NamedTableReferenceHandler()
        : base("NamedTableReference")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<SqlRowSet> Handle(NamedTableReference reference, SqlScope outer)
        => InMemorySelect.Named(reference, outer);
}
