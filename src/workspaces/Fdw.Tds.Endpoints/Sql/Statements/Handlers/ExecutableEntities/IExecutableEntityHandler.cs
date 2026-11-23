using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>Runs one kind of thing an EXEC names: a list of strings, or a procedure.</summary>
public interface IExecutableEntityHandler : ITypeOption<int, ExecutableEntityHandlerBase>
{
    /// <summary>Runs the entity.</summary>
    /// <param name="entity">An entity of the type this handler is named for.</param>
    /// <param name="context">The request.</param>
    /// <param name="cancellationToken">A token the client's attention signal cancels.</param>
    /// <returns>How it ended, or the refusal.</returns>
    Task<IGenericResult> Run(ExecutableEntity entity, SqlExecutionContext context, CancellationToken cancellationToken = default);
}
