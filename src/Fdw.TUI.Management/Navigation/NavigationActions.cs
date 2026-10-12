using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// TypeCollection for navigation actions.
/// </summary>
[TypeCollection(typeof(NavigationActionBase), typeof(INavigationAction), typeof(NavigationActions))]
[ExcludeFromCodeCoverage]
public abstract partial class NavigationActions : TypeCollectionBase<NavigationActionBase, INavigationAction> { }
