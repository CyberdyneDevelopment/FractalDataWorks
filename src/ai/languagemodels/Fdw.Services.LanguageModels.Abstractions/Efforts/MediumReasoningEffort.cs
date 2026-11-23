using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A moderate amount of reasoning.</summary>
[TypeOption(typeof(ReasoningEfforts), "Medium")]
[ExcludeFromCodeCoverage]
public sealed class MediumReasoningEffort : ReasoningEffortBase
{
    /// <summary>Initializes a new instance of the <see cref="MediumReasoningEffort"/> class.</summary>
    public MediumReasoningEffort() : base("Medium")
    {
    }
}
