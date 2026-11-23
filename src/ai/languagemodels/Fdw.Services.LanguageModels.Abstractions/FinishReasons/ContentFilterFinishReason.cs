using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The provider declined or cut the reply for its content rules.</summary>
[TypeOption(typeof(FinishReasons), "ContentFilter")]
[ExcludeFromCodeCoverage]
public sealed class ContentFilterFinishReason : FinishReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="ContentFilterFinishReason"/> class.</summary>
    public ContentFilterFinishReason() : base("ContentFilter", false)
    {
    }
}
