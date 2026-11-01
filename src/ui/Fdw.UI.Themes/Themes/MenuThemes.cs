using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Themes;

/// <summary>
/// TypeCollection of available menu themes.
/// </summary>
[TypeCollection(typeof(MenuThemeBase), typeof(IMenuTheme), typeof(MenuThemes))]
public partial class MenuThemes : TypeCollectionBase<MenuThemeBase, IMenuTheme>
{
}
