using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Connections.Abstractions.Messages;

/// <summary>
/// Collection definition to generate ConnectionMessages static class.
/// </summary>
[MessageCollection("ConnectionMessages", ReturnType = typeof(IServiceMessage))]
public abstract class ConnectionMessageCollectionBase : MessageCollectionBase<ConnectionMessage>
{

}