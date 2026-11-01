using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Connections.Abstractions.Messages;

namespace Fdw.Services.Connections.Http.Abstractions.Messages;

/// <summary>
/// Collection definition to generate HttpMessages static class.
/// </summary>
[MessageCollection("HttpMessages", ReturnType = typeof(IGenericMessage))]
public abstract class HttpMessageCollectionBase : MessageCollectionBase<ConnectionMessage>
{
}