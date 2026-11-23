using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description of a data option rule value's description: what one picked value means, in the words of whoever picked it (FDW-6208). Its owner is the rule. Read by whoever holds <c>datastores:read</c>, the permission the data option rule routes ask.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class DataConfigurationRuleValueDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the data option rule service writes and reads its descriptions under.</summary>
    public const string OptionName = "DataConfigurationRuleValue";

    /// <summary>Initializes a new instance of the <see cref="DataConfigurationRuleValueDescriptionType"/> class.</summary>
    public DataConfigurationRuleValueDescriptionType()
        : base(OptionName, "datastores:read", hasOwner: true)
    {
    }
}
