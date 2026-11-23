using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A lot of reasoning.</summary>
[TypeOption(typeof(ReasoningEfforts), "High")]
[ExcludeFromCodeCoverage]
public sealed class HighReasoningEffort : ReasoningEffortBase
{
    /// <summary>Initializes a new instance of the <see cref="HighReasoningEffort"/> class.</summary>
    public HighReasoningEffort() : base("High")
    {
    }
}
