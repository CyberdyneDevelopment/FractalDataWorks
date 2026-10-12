using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>Data only its owner and the people invited to it may reach; no broader grant applies.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataClassifications), "Restricted", RestrictToCurrentCompilation = true)]
public sealed class RestrictedDataClassificationOption : DataClassificationBase
{
    /// <summary>Initializes a new instance of the <see cref="RestrictedDataClassificationOption"/> class.</summary>
    public RestrictedDataClassificationOption()
        : base(
            name: "Restricted",
            sensitivity: 5,
            requiresInvitation: true,
            description: "Only the owner and people invited to it; no broader grant applies.")
    {
    }
}
