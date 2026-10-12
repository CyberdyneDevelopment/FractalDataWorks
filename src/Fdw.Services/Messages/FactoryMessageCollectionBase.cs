using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// Collection definition to generate FactoryMessages static class.
/// </summary>
[MessageCollection("FactoryMessages", ReturnType = typeof(IServiceMessage))]
public abstract class FactoryMessageCollectionBase : MessageCollectionBase<FactoryMessage>
{

}