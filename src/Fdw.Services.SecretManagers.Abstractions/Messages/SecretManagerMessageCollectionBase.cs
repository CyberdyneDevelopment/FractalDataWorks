using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.SecretManagers.Abstractions.Messages;

/// <summary>
/// Collection definition to generate SecretManagerMessages static class.
/// </summary>
[MessageCollection("SecretManagerMessages", ReturnType = typeof(IServiceMessage))]
public abstract class SecretManagerMessageCollectionBase : MessageCollectionBase<SecretManagerMessage>
{
}
