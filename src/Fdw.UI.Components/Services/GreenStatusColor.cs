using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>Green (healthy/success).</summary>
[TypeOption(typeof(StatusColors), "Green")]
[ExcludeFromCodeCoverage]
public sealed class GreenStatusColor : StatusColorBase
{
    /// <summary>Initializes a new instance of <see cref="GreenStatusColor"/>.</summary>
    public GreenStatusColor() : base(1, "Green", "dot-green", "var(--success)", "var(--success)") { }
}
