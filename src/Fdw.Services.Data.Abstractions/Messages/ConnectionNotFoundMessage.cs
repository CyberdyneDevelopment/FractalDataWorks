using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Data.Abstractions.Messages;

/// <summary>
/// Message indicating that a requested connection was not found.
/// </summary>
[Message("ConnectionNotFound")]
[MessageOption(typeof(DataGatewayMessageCollectionBase))]
public sealed class ConnectionNotFoundMessage : DataGatewayMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionNotFoundMessage"/> class.
    /// </summary>
    public ConnectionNotFoundMessage()
        : base(
            id: 1001,
            name: "ConnectionNotFound",
            severity: MessageSeverity.Error,
            message: "Connection '{0}' not found",
            code: "CONN_NOT_FOUND")
    {
    }
}
