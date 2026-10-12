using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Connections.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that the DataReader is closed and cannot be read.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("DataReaderClosed")]
[MessageOption(typeof(ConnectionMessageCollectionBase))]
public sealed class DataReaderClosedMessage : ConnectionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataReaderClosedMessage"/> class.
    /// </summary>
    public DataReaderClosedMessage()
        : base(1006, "DataReaderClosed", MessageSeverity.Error,
               "SqlDataReader is closed and cannot be read", "CONN_READER_CLOSED")
    { }
}
