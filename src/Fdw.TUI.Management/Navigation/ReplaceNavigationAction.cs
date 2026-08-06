using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.TUI.Management.Navigation;

/// <summary>Replace the current screen with a new one.</summary>
[TypeOption(typeof(NavigationActions), "Replace")]
[ExcludeFromCodeCoverage]
public sealed class ReplaceNavigationAction : NavigationActionBase
{
    /// <summary>Initializes a new instance of <see cref="ReplaceNavigationAction"/>.</summary>
    public ReplaceNavigationAction() : base(4, "Replace") { }
}
