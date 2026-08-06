using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Only field names must match; types may be different if convertible.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaCompatibilityModes), "Loose", RestrictToCurrentCompilation = true)]
public sealed class LooseMode : SchemaCompatibilityModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LooseMode"/> class.
    /// </summary>
    public LooseMode()
        : base(
            id: 5,
            name: "Loose",
            requiresExactTypes: false,
            allowsSourceExtras: true,
            allowsTargetExtras: true,
            validatesConstraints: false)
    {
    }
}
