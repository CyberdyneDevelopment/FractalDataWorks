using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>From the model.</summary>
[TypeOption(typeof(MessageRoles), "Assistant")]
[ExcludeFromCodeCoverage]
public sealed class AssistantMessageRole : MessageRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="AssistantMessageRole"/> class.</summary>
    public AssistantMessageRole() : base("Assistant")
    {
    }
}
