using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.TUI.Management.Navigation;

/// <summary>Exit the application.</summary>
[TypeOption(typeof(NavigationActions), "Exit")]
[ExcludeFromCodeCoverage]
public sealed class ExitNavigationAction : NavigationActionBase
{
    /// <summary>Initializes a new instance of <see cref="ExitNavigationAction"/>.</summary>
    public ExitNavigationAction() : base(5, "Exit") { }
}
