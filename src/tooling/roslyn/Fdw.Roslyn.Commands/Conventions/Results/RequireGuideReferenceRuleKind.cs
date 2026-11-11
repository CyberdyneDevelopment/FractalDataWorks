using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>The assembly must be among the project's references.</summary>
[TypeOption(typeof(GuideReferenceRuleKinds), "Require")]
[ExcludeFromCodeCoverage]
public sealed class RequireGuideReferenceRuleKind : GuideReferenceRuleKindBase
{
    /// <summary>Initializes a new instance of the <see cref="RequireGuideReferenceRuleKind"/> class.</summary>
    public RequireGuideReferenceRuleKind() : base(1, "Require") { }
}
