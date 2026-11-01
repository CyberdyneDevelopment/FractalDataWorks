using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>Data limited to the people whose work needs it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataClassifications), "Confidential", RestrictToCurrentCompilation = true)]
public sealed class ConfidentialDataClassificationOption : DataClassificationBase
{
    /// <summary>Initializes a new instance of the <see cref="ConfidentialDataClassificationOption"/> class.</summary>
    public ConfidentialDataClassificationOption()
        : base(
            name: "Confidential",
            sensitivity: 3,
            requiresInvitation: false,
            description: "Limited to the people whose work needs it.")
    {
    }
}
