using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Registration;

/// <summary>
/// TypeCollection for all UI page assemblies.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PageTypeBase), typeof(IPageType), typeof(PageTypes))]
public abstract partial class PageTypes : TypeCollectionBase<PageTypeBase, IPageType>
{
}
