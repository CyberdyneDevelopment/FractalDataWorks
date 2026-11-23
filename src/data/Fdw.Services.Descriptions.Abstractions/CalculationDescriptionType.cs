using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A calculation's description: what the calculation computes, shown beside its name in the list and the detail (FDW-6208). Read by whoever holds <c>calculation-entities:read</c>, the permission that reads the calculation.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class CalculationDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the calculation provider writes and reads its descriptions under.</summary>
    public const string OptionName = "Calculation";

    /// <summary>Initializes a new instance of the <see cref="CalculationDescriptionType"/> class.</summary>
    public CalculationDescriptionType()
        : base(OptionName, "calculation-entities:read", hasOwner: false)
    {
    }
}
