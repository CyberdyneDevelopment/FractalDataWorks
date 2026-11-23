using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>From the person or the host.</summary>
[TypeOption(typeof(MessageRoles), "User")]
[ExcludeFromCodeCoverage]
public sealed class UserMessageRole : MessageRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="UserMessageRole"/> class.</summary>
    public UserMessageRole() : base("User")
    {
    }
}
