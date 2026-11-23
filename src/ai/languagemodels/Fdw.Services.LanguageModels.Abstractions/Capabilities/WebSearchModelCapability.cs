using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The model searches the web itself.</summary>
[TypeOption(typeof(ModelCapabilities), "WebSearch")]
[ExcludeFromCodeCoverage]
public sealed class WebSearchModelCapability : ModelCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="WebSearchModelCapability"/> class.</summary>
    public WebSearchModelCapability() : base("WebSearch")
    {
    }
}
