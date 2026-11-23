using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The reply was cut at the output limit.</summary>
[TypeOption(typeof(FinishReasons), "Length")]
[ExcludeFromCodeCoverage]
public sealed class LengthFinishReason : FinishReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="LengthFinishReason"/> class.</summary>
    public LengthFinishReason() : base("Length", false)
    {
    }
}
