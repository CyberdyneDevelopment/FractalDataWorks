using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>What the host answers a tool call with.</summary>
[TypeOption(typeof(MessagePartKinds), "ToolResult")]
[ExcludeFromCodeCoverage]
public sealed class ToolResultMessagePartKind : MessagePartKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ToolResultMessagePartKind"/> class.</summary>
    public ToolResultMessagePartKind() : base("ToolResult", "Chat")
    {
    }
}
