using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.TUI.Management.Navigation;

/// <summary>Stay on the current screen.</summary>
[TypeOption(typeof(NavigationActions), "Stay")]
[ExcludeFromCodeCoverage]
public sealed class StayNavigationAction : NavigationActionBase
{
    /// <summary>Initializes a new instance of <see cref="StayNavigationAction"/>.</summary>
    public StayNavigationAction() : base(1, "Stay") { }
}
