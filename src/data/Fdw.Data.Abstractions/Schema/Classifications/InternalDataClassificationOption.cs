using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>Data for anyone in the organisation, not for publication.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataClassifications), "Internal", RestrictToCurrentCompilation = true)]
public sealed class InternalDataClassificationOption : DataClassificationBase
{
    /// <summary>Initializes a new instance of the <see cref="InternalDataClassificationOption"/> class.</summary>
    public InternalDataClassificationOption()
        : base(
            name: "Internal",
            sensitivity: 2,
            requiresInvitation: false,
            description: "For anyone in the organisation; not for publication.")
    {
    }
}
