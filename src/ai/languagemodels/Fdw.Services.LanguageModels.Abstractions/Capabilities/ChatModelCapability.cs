using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The model takes text and replies with text.</summary>
[TypeOption(typeof(ModelCapabilities), "Chat")]
[ExcludeFromCodeCoverage]
public sealed class ChatModelCapability : ModelCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="ChatModelCapability"/> class.</summary>
    public ChatModelCapability() : base("Chat")
    {
    }
}
