using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Connections.MsSql.Messages;

/// <summary>
/// Message indicating SQL command execution failed.
/// </summary>
[Message("SqlExecutionFailed")]
[MessageOption(typeof(MsSqlConnectionMessageCollectionBase))]
public sealed class SqlExecutionFailedMessage : MsSqlConnectionMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlExecutionFailedMessage"/> class.
    /// </summary>
    public SqlExecutionFailedMessage()
        : base(
            id: 2002,
            name: "SqlExecutionFailed",
            severity: MessageSeverity.Error,
            message: "SQL execution failed: {0}",
            code: "MSSQL_EXEC_FAILED")
    {
    }
}
