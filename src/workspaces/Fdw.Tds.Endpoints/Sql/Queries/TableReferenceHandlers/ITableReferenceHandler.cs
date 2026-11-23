using Fdw.Collections;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Rows;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>Reads the rows of one kind of FROM source whose sources are all in memory.</summary>
public interface ITableReferenceHandler : ITypeOption<int, TableReferenceHandlerBase>
{
    /// <summary>Reads a source.</summary>
    /// <param name="reference">A source of the type this handler is named for.</param>
    /// <param name="outer">The scope it runs in.</param>
    /// <returns>The rows, or the refusal.</returns>
    IGenericResult<SqlRowSet> Read(TableReference reference, SqlScope outer);
}
