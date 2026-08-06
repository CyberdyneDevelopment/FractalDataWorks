using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.TUI.Management.Navigation;

/// <summary>Pop the current screen off the stack.</summary>
[TypeOption(typeof(NavigationActions), "Pop")]
[ExcludeFromCodeCoverage]
public sealed class PopNavigationAction : NavigationActionBase
{
    /// <summary>Initializes a new instance of <see cref="PopNavigationAction"/>.</summary>
    public PopNavigationAction() : base(3, "Pop") { }
}
