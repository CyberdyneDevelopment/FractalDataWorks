using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A call a model asks the host to run.</summary>
[TypeOption(typeof(MessagePartKinds), "ToolCall")]
[ExcludeFromCodeCoverage]
public sealed class ToolCallMessagePartKind : MessagePartKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ToolCallMessagePartKind"/> class.</summary>
    public ToolCallMessagePartKind() : base("ToolCall", "Chat")
    {
    }
}
