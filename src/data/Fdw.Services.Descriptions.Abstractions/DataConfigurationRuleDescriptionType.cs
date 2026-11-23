using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description of a data option rule's description: what the rule is for, in the words of whoever made it (FDW-6208). The rule is the thing described, so the type has no owner. Read by whoever holds <c>datastores:read</c>, the permission the data option rule routes ask.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class DataConfigurationRuleDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the data option rule service writes and reads its descriptions under.</summary>
    public const string OptionName = "DataConfigurationRule";

    /// <summary>Initializes a new instance of the <see cref="DataConfigurationRuleDescriptionType"/> class.</summary>
    public DataConfigurationRuleDescriptionType()
        : base(OptionName, "datastores:read", hasOwner: false)
    {
    }
}
