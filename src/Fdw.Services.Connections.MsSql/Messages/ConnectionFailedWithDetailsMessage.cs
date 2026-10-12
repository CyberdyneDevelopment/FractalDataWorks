using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Connections.MsSql.Messages;

/// <summary>
/// Message indicating SQL Server connection failed with underlying error details.
/// </summary>
[Message("ConnectionFailedWithDetails")]
[MessageOption(typeof(MsSqlConnectionMessageCollectionBase))]
public sealed class ConnectionFailedWithDetailsMessage : MsSqlConnectionMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionFailedWithDetailsMessage"/> class.
    /// </summary>
    public ConnectionFailedWithDetailsMessage()
        : base(
            id: 2001,
            name: "ConnectionFailedWithDetails",
            severity: MessageSeverity.Error,
            message: "SQL Server connection failed: {0}",
            code: "MSSQL_CONN_FAILED")
    {
    }
}
