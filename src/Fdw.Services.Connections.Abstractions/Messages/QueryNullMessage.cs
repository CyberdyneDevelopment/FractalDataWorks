using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Connections.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that the query was null.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("QueryNull")]
[MessageOption(typeof(ConnectionMessageCollectionBase))]
public sealed class QueryNullMessage : ConnectionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryNullMessage"/> class.
    /// </summary>
    public QueryNullMessage()
        : base(1004, "QueryNull", MessageSeverity.Error,
               "Query cannot be null", "CONN_QUERY_NULL")
    { }
}
