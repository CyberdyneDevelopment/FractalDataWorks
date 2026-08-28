using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Connections.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that the connection ID was null or empty.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("ConnectionIdNullOrEmpty")]
[MessageOption(typeof(ConnectionMessageCollectionBase))]
public sealed class ConnectionIdNullOrEmptyMessage : ConnectionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionIdNullOrEmptyMessage"/> class.
    /// </summary>
    public ConnectionIdNullOrEmptyMessage()
        : base(1003, "ConnectionIdNullOrEmpty", MessageSeverity.Error,
               "Connection ID cannot be null or empty", "CONN_ID_NULL")
    { }
}
