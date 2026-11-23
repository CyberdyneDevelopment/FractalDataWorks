using Fdw.Collections;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>Base of the options of <see cref="HalfDoneRules"/>.</summary>
public abstract class HalfDoneRuleBase : TypeOptionBase<HalfDoneRuleBase>, IHalfDoneRule
{
    /// <summary>Initializes a new instance of the <see cref="HalfDoneRuleBase"/> class.</summary>
    /// <param name="name">The rule's name, the text a pipeline states.</param>
    /// <param name="description">What happens to the run.</param>
    protected HalfDoneRuleBase(string name, string description)
        : base(name, name, description, nameof(HalfDoneRules))
    {
    }
}
