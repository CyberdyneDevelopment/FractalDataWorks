using System.Data;
using Microsoft.Data.SqlClient;

namespace Fdw.Services.Connections.MsSql;

/// <summary>
/// One parameter a session context's statements use, described without being bound to a command.
/// </summary>
/// <param name="Name">The parameter's name, without the leading <c>@</c>, prefix included.</param>
/// <param name="Type">The SQL type the value is sent as.</param>
/// <param name="Value">The value.</param>
/// <remarks>
/// A <see cref="SqlParameter"/> belongs to the one command it was added to. A session context's statements
/// run either on their own or in the same batch as another command, so the statement carries this and each
/// command that runs it builds its own <see cref="SqlParameter"/>.
/// </remarks>
public sealed record SessionContextParameter(string Name, SqlDbType Type, object Value)
{
    /// <summary>Builds a parameter, named as the statement's SQL refers to it, for one command.</summary>
    /// <returns>A new parameter, not yet attached to any command.</returns>
    public SqlParameter ToSqlParameter() => new(string.Concat("@", Name), Type) { Value = Value };
}
