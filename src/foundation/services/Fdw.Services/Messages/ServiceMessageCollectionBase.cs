using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// Collection definition to generate ServiceMessages static class.
/// </summary>
[MessageCollection("ServiceMessages", ReturnType = typeof(IServiceMessage))]
public abstract class ServiceMessageCollectionBase : MessageCollectionBase<ServiceMessage>
{

}
