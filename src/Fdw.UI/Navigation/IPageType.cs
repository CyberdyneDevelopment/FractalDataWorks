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
    IReadOnlyList<Assembly> PageAssemblies { get; }
}
