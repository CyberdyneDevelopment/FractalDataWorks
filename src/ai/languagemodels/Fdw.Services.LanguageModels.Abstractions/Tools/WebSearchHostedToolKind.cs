using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A web search the provider runs.</summary>
[TypeOption(typeof(HostedToolKinds), "WebSearch")]
[ExcludeFromCodeCoverage]
public sealed class WebSearchHostedToolKind : HostedToolKindBase
{
    /// <summary>Initializes a new instance of the <see cref="WebSearchHostedToolKind"/> class.</summary>
    public WebSearchHostedToolKind() : base("WebSearch", "WebSearch")
    {
    }
}
