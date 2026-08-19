using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>Violet color — the tone the console sets apart from the semantic five, for a value that is
/// waiting on someone rather than succeeding, failing or running.</summary>
[TypeOption(typeof(StatusColors), "Violet")]
[ExcludeFromCodeCoverage]
public sealed class VioletStatusColor : StatusColorBase
{
    /// <summary>Initializes a new instance of <see cref="VioletStatusColor"/>.</summary>
    public VioletStatusColor() : base(6, "Violet", "dot-violet", "var(--violet)", "var(--violet)") { }
}
