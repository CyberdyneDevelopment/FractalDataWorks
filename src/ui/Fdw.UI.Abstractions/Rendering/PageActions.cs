using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>TypeCollection for page actions.</summary>
[TypeCollection(typeof(PageActionBase), typeof(IPageActionType), typeof(PageActions))]
[ExcludeFromCodeCoverage]
public abstract partial class PageActions : TypeCollectionBase<PageActionBase, IPageActionType> { }
