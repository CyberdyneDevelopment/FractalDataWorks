using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Data.Abstractions.Messages;

/// <summary>
/// Message indicating that connection retrieval failed.
/// </summary>
[Message("ConnectionRetrievalFailed")]
[MessageOption(typeof(DataGatewayMessageCollectionBase))]
public sealed class ConnectionRetrievalFailedMessage : DataGatewayMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionRetrievalFailedMessage"/> class.
    /// </summary>
    public ConnectionRetrievalFailedMessage()
        : base(
            id: 1002,
            name: "ConnectionRetrievalFailed",
            severity: MessageSeverity.Error,
            message: "Failed to retrieve connection '{0}'",
            code: "DG_CONN_RETRIEVAL_FAILED")
    {
    }
}
