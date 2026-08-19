using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>Slate color — the neutral one step brighter than <see cref="GrayStatusColor"/>, for a value
/// that stopped rather than one that is simply unknown.</summary>
[TypeOption(typeof(StatusColors), "Slate")]
[ExcludeFromCodeCoverage]
public sealed class SlateStatusColor : StatusColorBase
{
    /// <summary>Initializes a new instance of <see cref="SlateStatusColor"/>.</summary>
    public SlateStatusColor() : base(7, "Slate", "dot-glacier", "var(--n-400)", "var(--n-500)") { }
}
