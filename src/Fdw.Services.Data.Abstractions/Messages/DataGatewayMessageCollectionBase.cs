using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Data.Abstractions.Messages;

/// <summary>
/// Collection base for data gateway messages.
/// Generates static factory methods in DataGatewayMessages class.
/// </summary>
[MessageCollection("DataGatewayMessages")]
public abstract class DataGatewayMessageCollectionBase : MessageCollectionBase<DataGatewayMessage>
{
}
