using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>Data that may be published outside the organisation.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataClassifications), "Public", RestrictToCurrentCompilation = true)]
public sealed class PublicDataClassificationOption : DataClassificationBase
{
    /// <summary>Initializes a new instance of the <see cref="PublicDataClassificationOption"/> class.</summary>
    public PublicDataClassificationOption()
        : base(
            name: "Public",
            sensitivity: 1,
            requiresInvitation: false,
            description: "May be published outside the organisation.")
    {
    }
}
