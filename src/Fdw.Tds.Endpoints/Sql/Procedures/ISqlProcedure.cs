using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Procedures;

/// <summary>
/// A stored procedure this endpoint runs, by RPC or by EXEC.
/// </summary>
public interface ISqlProcedure : ITypeOption<int, ISqlProcedure>
{
    /// <summary>Gets the procedure's name, such as sp_executesql.</summary>
    string SqlName { get; }

    /// <summary>Runs one call.</summary>
    /// <param name="call">The call.</param>
    /// <param name="cancellationToken">A token the client's attention signal cancels.</param>
    /// <returns>The OUTPUT values, or the refusal.</returns>
    Task<IGenericResult<SqlProcedureResult>> Execute(SqlProcedureCall call, CancellationToken cancellationToken = default);
}
