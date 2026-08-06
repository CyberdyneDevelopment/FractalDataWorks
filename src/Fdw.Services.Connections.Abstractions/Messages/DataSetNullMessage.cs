using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Connections.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that the DataSet was null.
/// </summary>
// Why: pure message DTO; ctor only forwards literal id/severity/text to the base template, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("DataSetNull")]
[MessageOption(typeof(ConnectionMessageCollectionBase))]
public sealed class DataSetNullMessage : ConnectionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetNullMessage"/> class.
    /// </summary>
    public DataSetNullMessage()
        : base(1005, "DataSetNull", MessageSeverity.Error,
               "DataSet cannot be null", "CONN_DATASET_NULL")
    { }
}
