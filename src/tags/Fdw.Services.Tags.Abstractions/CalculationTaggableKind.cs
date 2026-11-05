using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>A Calculation that carries tags in <c>calc.CalculationTag</c>. A thing of this kind sits in a workspace, so an organization or workspace tag is checked against its place.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TaggableKinds), "Calculation")]
public sealed class CalculationTaggableKind : TaggableKindBase
{
    /// <summary>Initializes a new instance of the <see cref="CalculationTaggableKind"/> class.</summary>
    public CalculationTaggableKind()
        : base("Calculation", "calc", "CalculationTag", "CalculationId", "Calculation")
    {
    }
}
