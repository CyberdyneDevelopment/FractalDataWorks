using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>One value of <see cref="GuideReferenceRuleKinds"/>: whether a reference rule requires or forbids an assembly.</summary>
public interface IGuideReferenceRuleKind : ITypeOption<int, GuideReferenceRuleKindBase> { }
