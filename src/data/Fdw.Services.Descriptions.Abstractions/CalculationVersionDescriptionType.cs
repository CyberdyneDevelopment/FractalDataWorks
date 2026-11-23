using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A calculation version's description: what the calculation is in the words of the organization or workspace the version is held at (FDW-6208). Read by whoever holds <c>calculation-entities:read</c>, the permission the versions list asks. The version is the thing described, so the type has no owner: a version is removed with its own descriptions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class CalculationVersionDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the calculation version provider writes and reads its descriptions under.</summary>
    public const string OptionName = "CalculationVersion";

    /// <summary>Initializes a new instance of the <see cref="CalculationVersionDescriptionType"/> class.</summary>
    public CalculationVersionDescriptionType()
        : base(OptionName, "calculation-entities:read", hasOwner: false)
    {
    }
}
