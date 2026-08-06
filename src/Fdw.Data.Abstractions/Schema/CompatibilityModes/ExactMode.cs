using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Schemas must be identical in structure and constraints.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaCompatibilityModes), "Exact", RestrictToCurrentCompilation = true)]
public sealed class ExactMode : SchemaCompatibilityModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExactMode"/> class.
    /// </summary>
    public ExactMode()
        : base(
            id: 1,
            name: "Exact",
            requiresExactTypes: true,
            allowsSourceExtras: false,
            allowsTargetExtras: false,
            validatesConstraints: true)
    {
    }
}
