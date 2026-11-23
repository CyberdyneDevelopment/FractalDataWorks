using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description of a quality rule: what the rule checks and why, in the words of whoever made it (FDW-6208). The rule is the thing described, so the type has no owner. Read by whoever holds <c>quality/rules:read</c>, the permission the quality rule routes ask.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class QualityRuleDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the owning domain's provider writes and reads its descriptions under.</summary>
    public const string OptionName = "QualityRule";

    /// <summary>Initializes a new instance of the <see cref="QualityRuleDescriptionType"/> class.</summary>
    public QualityRuleDescriptionType()
        : base(OptionName, "quality/rules:read", hasOwner: false)
    {
    }
}
