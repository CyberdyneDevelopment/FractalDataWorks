using Fdw.Results;
using Fdw.Tds.Endpoints.Hosting;

namespace Fdw.Tds.Endpoints.Sql.Procedures.BuiltIns;

/// <summary>
/// Keeps the statement sp_prepare and sp_prepexec name: @params at position 1, @stmt at position 2.
/// </summary>
internal static class Prepared
{
    /// <summary>Keeps a call's statement on the session.</summary>
    /// <param name="call">The call.</param>
    /// <returns>The handle and the statement kept, or the refusal.</returns>
    public static IGenericResult<(int Handle, PreparedStatement Statement)> Keep(SqlProcedureCall call)
    {
        var declarations = ProcedureArguments.Text(call, "@params", 1, required: false);
        if (declarations.IsFailure)
            return declarations.ToNewResult<(int, PreparedStatement)>();

        var statement = ProcedureArguments.Text(call, "@stmt", 2, required: true);
        if (statement.IsFailure)
            return statement.ToNewResult<(int, PreparedStatement)>();

        var prepared = new PreparedStatement(declarations.Value!, statement.Value!);
        return GenericResult<(int, PreparedStatement)>.Success((call.Context.Session.Prepare(prepared), prepared));
    }
}
