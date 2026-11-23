using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A little reasoning.</summary>
[TypeOption(typeof(ReasoningEfforts), "Low")]
[ExcludeFromCodeCoverage]
public sealed class LowReasoningEffort : ReasoningEffortBase
{
    /// <summary>Initializes a new instance of the <see cref="LowReasoningEffort"/> class.</summary>
    public LowReasoningEffort() : base("Low")
    {
    }
}
