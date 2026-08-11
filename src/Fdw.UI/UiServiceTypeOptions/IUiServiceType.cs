using System.Collections.Generic;
using Fdw.ServiceTypes;
using Fdw.UI.ComponentOptions;

namespace Fdw.UI.UiServiceTypeOptions;

/// <summary>
/// A domain's UI surface: the component collections it owns, driven through the three phases.
/// </summary>
public interface IUiServiceType : IServiceType
{
    /// <summary>
    /// Gets the component collections this domain owns.
    /// </summary>
    /// <remarks>
    /// Named by the service type rather than discovered. Discovery would mean scanning for every
    /// IComponentTypeCollection in the process, which is the assembly scanning this mechanism
    /// replaces, and would pull in components belonging to a domain the skin never asked for.
    /// </remarks>
    IReadOnlyList<IComponentTypeCollection> ComponentCollections { get; }
}
