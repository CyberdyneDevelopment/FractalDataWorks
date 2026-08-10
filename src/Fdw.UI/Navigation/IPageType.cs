using System.Collections.Generic;
using System.Reflection;
using Fdw.Collections;

namespace Fdw.UI.Navigation;

/// <summary>
/// A group of related pages contributed to <see cref="PageTypes"/> by one package.
/// </summary>
public interface IPageType : ITypeOption<int, IPageType>
{
    /// <summary>
    /// Gets the pages this group contributes, each carrying its own sidebar entry.
    /// </summary>
    IReadOnlyList<IPage> Pages { get; }

    /// <summary>
    /// Gets the distinct assemblies the declared pages live in, for the renderer's route discovery.
    /// </summary>
    // Why: DERIVED from Pages, not declared. The previous hand-declared Assembly could name an assembly
    // holding none of the declared pages, and it forced one page type per assembly even after the package
    // reorg put many groups in ONE assembly — which is why the Blazor Router consumer had to Distinct()
    // the result to avoid "Assembly already defined".
    IReadOnlyList<Assembly> PageAssemblies { get; }
}
