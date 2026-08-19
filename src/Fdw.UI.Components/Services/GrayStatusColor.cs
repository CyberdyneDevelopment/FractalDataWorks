using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>Gray (unknown/disabled).</summary>
[TypeOption(typeof(StatusColors), "Gray")]
[ExcludeFromCodeCoverage]
public sealed class GrayStatusColor : StatusColorBase
{
    /// <summary>Initializes a new instance of <see cref="GrayStatusColor"/>.</summary>
    public GrayStatusColor() : base(5, "Gray", "dot-glacier", "var(--n-500)", "var(--n-600)") { }
}
