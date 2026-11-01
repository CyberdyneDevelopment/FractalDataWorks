using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// TypeCollection of available menu targets.
/// Use MenuTargets.ByName() for dispatch-based menu navigation.
/// </summary>
[TypeCollection(typeof(MenuTargetBase), typeof(IMenuTarget), typeof(MenuTargets))]
public partial class MenuTargets : TypeCollectionBase<MenuTargetBase, IMenuTarget>
{
}
