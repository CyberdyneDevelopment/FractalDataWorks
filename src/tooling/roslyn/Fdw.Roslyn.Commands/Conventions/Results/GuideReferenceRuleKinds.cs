using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>TypeCollection of whether a reference rule requires or forbids an assembly.</summary>
[TypeCollection(typeof(GuideReferenceRuleKindBase), typeof(IGuideReferenceRuleKind), typeof(GuideReferenceRuleKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class GuideReferenceRuleKinds : TypeCollectionBase<GuideReferenceRuleKindBase, IGuideReferenceRuleKind> { }
