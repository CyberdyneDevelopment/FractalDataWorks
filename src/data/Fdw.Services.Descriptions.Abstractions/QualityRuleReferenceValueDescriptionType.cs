using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description of one reference value of a quality rule: what the value means, in the words of whoever stated it (FDW-6208). Its owner is the rule. Read by whoever holds <c>quality/rules:read</c>, the permission the quality rule routes ask.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class QualityRuleReferenceValueDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the owning domain's provider writes and reads its descriptions under.</summary>
    public const string OptionName = "QualityRuleReferenceValue";

    /// <summary>Initializes a new instance of the <see cref="QualityRuleReferenceValueDescriptionType"/> class.</summary>
    public QualityRuleReferenceValueDescriptionType()
        : base(OptionName, "quality/rules:read", hasOwner: true)
    {
    }
}
