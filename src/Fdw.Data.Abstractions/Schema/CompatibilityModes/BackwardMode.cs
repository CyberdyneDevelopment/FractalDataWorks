using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// All fields in the target schema must exist in the source schema with compatible types.
/// The source schema may have additional fields.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaCompatibilityModes), "Backward", RestrictToCurrentCompilation = true)]
public sealed class BackwardMode : SchemaCompatibilityModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackwardMode"/> class.
    /// </summary>
    public BackwardMode()
        : base(
            id: 2,
            name: "Backward",
            requiresExactTypes: true,
            allowsSourceExtras: true,
            allowsTargetExtras: false,
            validatesConstraints: true)
    {
    }
}
