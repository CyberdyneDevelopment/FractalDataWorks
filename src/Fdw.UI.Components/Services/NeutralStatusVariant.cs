using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>Neutral variant.</summary>
[TypeOption(typeof(StatusVariants), "Neutral")]
[ExcludeFromCodeCoverage]
public sealed class NeutralStatusVariant : StatusVariantBase
{
    /// <summary>Initializes a new instance of <see cref="NeutralStatusVariant"/>.</summary>
    public NeutralStatusVariant() : base(5, "Neutral") { }
}
