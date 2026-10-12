using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>Personal or regulated data, whose disclosure causes harm.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataClassifications), "Protected", RestrictToCurrentCompilation = true)]
public sealed class ProtectedDataClassificationOption : DataClassificationBase
{
    /// <summary>Initializes a new instance of the <see cref="ProtectedDataClassificationOption"/> class.</summary>
    public ProtectedDataClassificationOption()
        : base(
            name: "Protected",
            sensitivity: 4,
            requiresInvitation: false,
            description: "Personal or regulated data; disclosure causes harm.")
    {
    }
}
