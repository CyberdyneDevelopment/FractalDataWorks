using System.Collections.Generic;
using Fdw.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace Fdw.Web.RestEndpoints.ApiServiceTypeOptions;

/// <summary>
/// A domain's API surface: the endpoint collections it owns, driven through the three phases.
/// </summary>
public interface IApiServiceType : IServiceType
{
    /// <summary>
    /// Gets the endpoint collections this domain owns.
    /// </summary>
    /// <remarks>
    /// Named by the service type rather than discovered. Discovery would mean scanning for every
    /// IEndpointTypeCollection in the process, which is the assembly scanning this whole mechanism
    /// replaces — and it would silently pull in collections belonging to a domain the host never
    /// asked for.
    /// </remarks>
    IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; }
}
