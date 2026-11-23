using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A threshold's description: what the threshold judges and what it does, in the words of whoever made it (FDW-7205). Read by whoever holds <c>thresholds:read</c>, the permission the threshold routes ask. The threshold is the thing described, so the type has no owner: a threshold is removed with its own descriptions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class ThresholdDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the threshold provider writes and reads its descriptions under.</summary>
    public const string OptionName = "Threshold";

    /// <summary>Initializes a new instance of the <see cref="ThresholdDescriptionType"/> class.</summary>
    public ThresholdDescriptionType()
        : base(OptionName, "thresholds:read", hasOwner: false)
    {
    }
}
