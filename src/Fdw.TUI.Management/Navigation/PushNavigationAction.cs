using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.TUI.Management.Navigation;

/// <summary>Push a new screen onto the navigation stack.</summary>
[TypeOption(typeof(NavigationActions), "Push")]
[ExcludeFromCodeCoverage]
public sealed class PushNavigationAction : NavigationActionBase
{
    /// <summary>Initializes a new instance of <see cref="PushNavigationAction"/>.</summary>
    public PushNavigationAction() : base(2, "Push") { }
}
