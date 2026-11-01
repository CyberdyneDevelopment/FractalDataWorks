using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Procedures;

/// <summary>
/// Base class for the stored procedures this endpoint runs.
/// </summary>
public abstract class SqlProcedureBase : TypeOptionBase<SqlProcedureBase>, ISqlProcedure
{
    /// <summary>Initializes a new instance of the <see cref="SqlProcedureBase"/> class.</summary>
    /// <param name="name">The option's name; must match its TypeOption attribute.</param>
    /// <param name="sqlName">The procedure's name.</param>
    protected SqlProcedureBase(string name, string sqlName)
        : base(name)
    {
        SqlName = sqlName;
    }

    /// <inheritdoc/>
    public string SqlName { get; }

    /// <inheritdoc/>
    public abstract Task<IGenericResult<SqlProcedureResult>> Execute(SqlProcedureCall call, CancellationToken cancellationToken = default);
}
