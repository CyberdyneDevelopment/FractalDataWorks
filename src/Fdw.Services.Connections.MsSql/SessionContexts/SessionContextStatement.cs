using System.Collections.Generic;

namespace Fdw.Services.Connections.MsSql;

/// <summary>
/// The SQL a session context runs to set its <c>SESSION_CONTEXT</c> keys on a connection, and the
/// parameters it uses.
/// </summary>
/// <param name="Sql">
/// The <c>EXEC sp_set_session_context</c> statements, each ending in a semicolon and each setting its key
/// <c>@read_only = 1</c>.
/// </param>
/// <param name="Parameters">The parameters <paramref name="Sql"/> refers to.</param>
/// <remarks>
/// This is the one definition of what a session context sets. <see cref="MsSqlSessionContextBase.Apply"/>
/// runs <see cref="Sql"/> on its own, and <see cref="BatchedSessionContext"/> sends <see cref="Guarded"/> in
/// the same batch as a command, so the two cannot set different keys.
/// </remarks>
public sealed record SessionContextStatement(string Sql, IReadOnlyList<SessionContextParameter> Parameters)
{
    /// <summary>
    /// Gets <see cref="Sql"/> wrapped so that a failure to set a key ends the batch, and the command that
    /// follows it never runs. Without the wrapper T-SQL would go on to the next statement, so a command
    /// could run with none of its context keys, or with only some of them (a system elevation with its
    /// tenant key missing), under a row-level-security predicate written for the whole set.
    /// </summary>
    public string Guarded => string.Concat("BEGIN TRY ", Sql, " END TRY BEGIN CATCH THROW; END CATCH; ");
}
