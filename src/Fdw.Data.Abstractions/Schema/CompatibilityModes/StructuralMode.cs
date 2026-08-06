using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Field names and types must match, but constraints may differ.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaCompatibilityModes), "Structural", RestrictToCurrentCompilation = true)]
public sealed class StructuralMode : SchemaCompatibilityModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StructuralMode"/> class.
    /// </summary>
    public StructuralMode()
        : base(
            id: 4,
            name: "Structural",
            requiresExactTypes: true,
            allowsSourceExtras: false,
            allowsTargetExtras: false,
            validatesConstraints: false)
    {
    }
}
