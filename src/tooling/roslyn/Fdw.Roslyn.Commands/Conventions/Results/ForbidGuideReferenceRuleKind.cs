using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>The assembly must not be among the project's references.</summary>
[TypeOption(typeof(GuideReferenceRuleKinds), "Forbid")]
[ExcludeFromCodeCoverage]
public sealed class ForbidGuideReferenceRuleKind : GuideReferenceRuleKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ForbidGuideReferenceRuleKind"/> class.</summary>
    public ForbidGuideReferenceRuleKind() : base(2, "Forbid") { }
}
