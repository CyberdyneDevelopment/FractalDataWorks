using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>Success variant.</summary>
[TypeOption(typeof(StatusVariants), "Success")]
[ExcludeFromCodeCoverage]
public sealed class SuccessStatusVariant : StatusVariantBase
{
    /// <summary>Initializes a new instance of <see cref="SuccessStatusVariant"/>.</summary>
    public SuccessStatusVariant() : base(1, "Success") { }
}
