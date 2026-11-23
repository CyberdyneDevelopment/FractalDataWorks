using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The model finished its reply.</summary>
[TypeOption(typeof(FinishReasons), "Completed")]
[ExcludeFromCodeCoverage]
public sealed class CompletedFinishReason : FinishReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="CompletedFinishReason"/> class.</summary>
    public CompletedFinishReason() : base("Completed", false)
    {
    }
}
