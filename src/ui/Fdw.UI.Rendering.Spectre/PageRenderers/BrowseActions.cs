using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>
/// The actions the user can take on a browse page (FDW-4152): drill down, back, refresh and quit. The browse prompt lists
/// the options that state a menu label and answers the option the user chose.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(BrowseActionBase), typeof(IBrowseAction), typeof(BrowseActions))]
public abstract partial class BrowseActions : TypeCollectionBase<BrowseActionBase, IBrowseAction>
{
}
