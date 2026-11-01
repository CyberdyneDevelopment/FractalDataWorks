using System;
using System.Collections.Generic;

namespace Fdw.Tds.Endpoints.Sql.Procedures;

/// <summary>
/// How a procedure ended: the values it hands back through OUTPUT arguments.
/// </summary>
public sealed class SqlProcedureResult
{
    /// <summary>Initializes a new instance of the <see cref="SqlProcedureResult"/> class.</summary>
    /// <param name="outputs">The OUTPUT values, in argument order.</param>
    public SqlProcedureResult(IReadOnlyList<SqlProcedureOutput> outputs)
    {
        Outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
    }

    /// <summary>Gets the OUTPUT values.</summary>
    public IReadOnlyList<SqlProcedureOutput> Outputs { get; }

    /// <summary>A procedure that hands nothing back.</summary>
    /// <returns>The result.</returns>
    public static SqlProcedureResult Nothing() => new([]);
}
