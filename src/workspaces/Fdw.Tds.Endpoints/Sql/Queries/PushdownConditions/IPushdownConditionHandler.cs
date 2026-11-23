using Fdw.Collections;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>Translates one kind of WHERE condition into the filter tree a data set's store runs.</summary>
public interface IPushdownConditionHandler : ITypeOption<int, PushdownConditionHandlerBase>
{
    /// <summary>Translates a condition.</summary>
    /// <param name="condition">A condition of the type this handler is named for.</param>
    /// <param name="table">The table it filters.</param>
    /// <param name="qualifier">The alias or name the query qualifies the table's columns by.</param>
    /// <param name="scope">The scope its values are evaluated in.</param>
    /// <returns>The filter tree, or the refusal.</returns>
    IGenericResult<QueryFilterNode> Translate(BooleanExpression condition, SqlTable table, string qualifier, SqlScope scope);
}
