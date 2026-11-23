using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// Base class for the options of <see cref="MessageRoles"/>: who a message in a conversation is from.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class MessageRoleBase : TypeOptionBase<MessageRoleBase>, IMessageRole
{
    /// <summary>Initializes a new instance of the <see cref="MessageRoleBase"/> class.</summary>
    /// <param name="name">The name of the option.</param>
    protected MessageRoleBase(string name) : base(name)
    {
    }
}
