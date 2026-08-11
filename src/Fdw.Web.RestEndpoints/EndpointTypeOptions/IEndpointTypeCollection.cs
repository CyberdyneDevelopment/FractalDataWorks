using System.Collections.Generic;
using Fdw.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.EndpointTypeOptions;

/// <summary>
/// An endpoint collection a service type can drive through the three phases without naming it.
/// </summary>
/// <remarks>
/// This interface exists because <c>All()</c> is a generated STATIC on each derived collection, so
/// no base and no service type can call it generically. A service type holding
/// <c>IEnumerable&lt;IEndpointTypeCollection&gt;</c> can cycle every resource it owns; without it,
/// every service type would have to name each collection by hand and would silently miss one added
/// later.
///
/// Each concrete collection satisfies <see cref="Members"/> by returning its own generated
/// <c>All()</c>. That one line per collection is the bridge between the static generated surface
/// and the polymorphism the registration sweep needs.
/// </remarks>
public interface IEndpointTypeCollection
{
    /// <summary>
    /// Gets or sets a value indicating whether this whole resource should be passed over.
    /// </summary>
    bool SkipRegistration { get; set; }

    /// <summary>
    /// Gets the endpoints declared against this collection, skipped ones included.
    /// </summary>
    /// <remarks>
    /// Filtering happens during the phases, not here, so a caller enumerating this sees everything
    /// declared — which is what a diagnostic surface listing "what exists and what is switched off"
    /// needs.
    /// </remarks>
    IEnumerable<IEndpointTypeOption> Members { get; }

    /// <summary>Runs Configure for this resource, then for each endpoint not skipped.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The builder, or the first failure encountered.</returns>
    IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder);

    /// <summary>Runs Register for this resource, then for each endpoint not skipped.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The logger factory, if the host has one yet.</param>
    /// <returns>The builder, or the first failure encountered.</returns>
    IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null);

    /// <summary>Runs Initialize for this resource, then for each endpoint not skipped.</summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The host, or the first failure encountered.</returns>
    IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null);
}
