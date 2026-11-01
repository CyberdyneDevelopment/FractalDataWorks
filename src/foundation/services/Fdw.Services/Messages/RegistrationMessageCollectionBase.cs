using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// Collection definition to generate RegistrationMessages static class.
/// </summary>
[MessageCollection("RegistrationMessages", ReturnType = typeof(IServiceMessage))]
public abstract class RegistrationMessageCollectionBase : MessageCollectionBase<RegistrationMessage>
{

}