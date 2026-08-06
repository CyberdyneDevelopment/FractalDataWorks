using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// All fields in the source schema must exist in the target schema with compatible types.
/// The target schema may have additional fields.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaCompatibilityModes), "Forward", RestrictToCurrentCompilation = true)]
public sealed class ForwardMode : SchemaCompatibilityModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForwardMode"/> class.
    /// </summary>
    public ForwardMode()
        : base(
            id: 3,
            name: "Forward",
            requiresExactTypes: true,
            allowsSourceExtras: false,
            allowsTargetExtras: true,
            validatesConstraints: true)
    {
    }
}
