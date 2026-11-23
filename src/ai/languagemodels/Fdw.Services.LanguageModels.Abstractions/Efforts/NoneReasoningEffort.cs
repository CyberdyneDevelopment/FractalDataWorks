using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>No reasoning: the model replies without a reasoning pass.</summary>
[TypeOption(typeof(ReasoningEfforts), "None")]
[ExcludeFromCodeCoverage]
public sealed class NoneReasoningEffort : ReasoningEffortBase
{
    /// <summary>Initializes a new instance of the <see cref="NoneReasoningEffort"/> class.</summary>
    public NoneReasoningEffort() : base("None")
    {
    }
}
