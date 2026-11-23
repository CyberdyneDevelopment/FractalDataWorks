using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A call the model asks the host to run.</summary>
[TypeOption(typeof(StreamEventKinds), "ToolCall")]
[ExcludeFromCodeCoverage]
public sealed class ToolCallStreamEventKind : StreamEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ToolCallStreamEventKind"/> class.</summary>
    public ToolCallStreamEventKind() : base("ToolCall", false)
    {
    }
}
