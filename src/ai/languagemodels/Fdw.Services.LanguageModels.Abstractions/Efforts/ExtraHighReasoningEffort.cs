using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The most reasoning a provider offers.</summary>
[TypeOption(typeof(ReasoningEfforts), "ExtraHigh")]
[ExcludeFromCodeCoverage]
public sealed class ExtraHighReasoningEffort : ReasoningEffortBase
{
    /// <summary>Initializes a new instance of the <see cref="ExtraHighReasoningEffort"/> class.</summary>
    public ExtraHighReasoningEffort() : base("ExtraHigh")
    {
    }
}
