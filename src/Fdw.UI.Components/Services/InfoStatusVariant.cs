using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>Info variant.</summary>
[TypeOption(typeof(StatusVariants), "Info")]
[ExcludeFromCodeCoverage]
public sealed class InfoStatusVariant : StatusVariantBase
{
    /// <summary>Initializes a new instance of <see cref="InfoStatusVariant"/>.</summary>
    public InfoStatusVariant() : base(4, "Info", "b-run") { }
}
