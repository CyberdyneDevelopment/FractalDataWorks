using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Themes;

/// <summary>
/// TypeCollection of available icon sets.
/// </summary>
[TypeCollection(typeof(IconSetBase), typeof(IIconSet), typeof(IconSets))]
public partial class IconSets : TypeCollectionBase<IconSetBase, IIconSet>
{
}
