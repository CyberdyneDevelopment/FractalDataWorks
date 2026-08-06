using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Themes;

/// <summary>
/// TypeCollection of available border styles.
/// </summary>
[TypeCollection(typeof(BorderStyleBase), typeof(IBorderStyle), typeof(BorderStyles))]
public partial class BorderStyles : TypeCollectionBase<BorderStyleBase, IBorderStyle>
{
}
