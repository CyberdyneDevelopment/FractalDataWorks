using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>Red (error/unhealthy).</summary>
[TypeOption(typeof(StatusColors), "Red")]
[ExcludeFromCodeCoverage]
public sealed class RedStatusColor : StatusColorBase
{
    /// <summary>Initializes a new instance of <see cref="RedStatusColor"/>.</summary>
    public RedStatusColor() : base(2, "Red") { }
}
