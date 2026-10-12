using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Messages;

/// <summary>
/// Collection definition to generate AuthenticationMessages static class.
/// </summary>
[MessageCollection("AuthenticationMessages", ReturnType = typeof(IServiceMessage))]
public abstract class AuthenticationMessageCollectionBase : MessageCollectionBase<AuthenticationMessage>
{

}