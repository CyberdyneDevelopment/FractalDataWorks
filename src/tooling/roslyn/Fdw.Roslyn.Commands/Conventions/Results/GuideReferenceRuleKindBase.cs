using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>Base of every <see cref="GuideReferenceRuleKinds"/> option: whether a reference rule requires or forbids an assembly.</summary>
public abstract class GuideReferenceRuleKindBase : TypeOptionBase<GuideReferenceRuleKindBase>, IGuideReferenceRuleKind
{
    /// <summary>Initializes a new instance of the <see cref="GuideReferenceRuleKindBase"/> class.</summary>
    /// <param name="id">The option's id; ids start at 1, since 0 is the collection's not-found value.</param>
    /// <param name="name">The option's name, as the results write it.</param>
    protected GuideReferenceRuleKindBase(int id, string name) : base(id, name) { }
}
