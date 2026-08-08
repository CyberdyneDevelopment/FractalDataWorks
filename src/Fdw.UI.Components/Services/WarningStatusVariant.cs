using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>Warning variant.</summary>
[TypeOption(typeof(StatusVariants), "Warning")]
[ExcludeFromCodeCoverage]
public sealed class WarningStatusVariant : StatusVariantBase
{
    /// <summary>Initializes a new instance of <see cref="WarningStatusVariant"/>.</summary>
    public WarningStatusVariant() : base(3, "Warning", "b-warn") { }
}
