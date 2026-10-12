using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// TypeCollection of page modes (View, Create, Edit).
/// </summary>
[TypeCollection(typeof(PageModeBase), typeof(IPageMode), typeof(PageModes))]
public partial class PageModes : TypeCollectionBase<PageModeBase, IPageMode>
{
}
