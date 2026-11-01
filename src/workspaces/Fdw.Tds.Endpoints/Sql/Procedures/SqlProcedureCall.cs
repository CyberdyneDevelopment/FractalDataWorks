using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Tds.Endpoints.Sql.Procedures;

/// <summary>
/// One call of a procedure: the request it runs in and the arguments it was given.
/// </summary>
public sealed class SqlProcedureCall
{
    /// <summary>Initializes a new instance of the <see cref="SqlProcedureCall"/> class.</summary>
    /// <param name="context">The request.</param>
    /// <param name="procedure">The procedure's name, for the errors that name it.</param>
    /// <param name="arguments">The arguments, in call order.</param>
    /// <param name="line">The line the call was written on; 1 for an RPC.</param>
    public SqlProcedureCall(SqlExecutionContext context, string procedure, IReadOnlyList<SqlProcedureArgument> arguments, int line)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Procedure = procedure ?? throw new ArgumentNullException(nameof(procedure));
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        Line = line;
    }

    /// <summary>Gets the request.</summary>
    public SqlExecutionContext Context { get; }

    /// <summary>Gets the procedure's name.</summary>
    public string Procedure { get; }

    /// <summary>Gets the arguments, in call order.</summary>
    public IReadOnlyList<SqlProcedureArgument> Arguments { get; }

    /// <summary>Gets the line the call was written on.</summary>
    public int Line { get; }

    /// <summary>
    /// Finds the argument a procedure's parameter takes: the one passed by that name, or else the one at that
    /// position when it was passed by position.
    /// </summary>
    /// <param name="name">The parameter's name, with its @.</param>
    /// <param name="position">The parameter's position, from 0.</param>
    /// <param name="argument">The argument, when one was passed.</param>
    /// <returns>True when one was passed.</returns>
    public bool TryArgument(string name, int position, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SqlProcedureArgument? argument)
    {
        argument = Arguments.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
        if (argument is null && position < Arguments.Count && Arguments[position].Name.Length == 0)
            argument = Arguments[position];

        return argument is not null;
    }
}
