using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// Base class for the options of <see cref="ReasoningEfforts"/>: how much reasoning a model does before it replies, stated per call.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ReasoningEffortBase : TypeOptionBase<ReasoningEffortBase>, IReasoningEffort
{
    /// <summary>Initializes a new instance of the <see cref="ReasoningEffortBase"/> class.</summary>
    /// <param name="name">The name of the option.</param>
    protected ReasoningEffortBase(string name) : base(name)
    {
    }
}
