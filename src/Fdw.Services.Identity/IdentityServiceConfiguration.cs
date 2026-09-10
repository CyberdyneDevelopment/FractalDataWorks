using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Identity.Abstractions;

namespace Fdw.Services.Identity;

/// <summary>
/// Header configuration for identity services representing the <c>sec.Identity</c> parent table —
/// the identities this process can assume when calling out to a peer.
/// </summary>
/// <remarks>
/// <para>
/// Identity-only, per the polymorphic configuration pattern: everything a factory reads at runtime
/// lives on the typed body, because runtime dispatch reads only the typed body and parent fields are
/// discarded after dispatch.
/// </para>
/// <para>
/// After loading a header row, <c>IdentityServiceConfigurationProvider</c> dispatches to the typed
/// body provider and sets <see cref="Configuration"/>. Callers read typed fields by casting, e.g.
/// <c>(header.Configuration as ClientCredentialsConfiguration)</c>.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Identity")]
public partial class IdentityServiceConfiguration : DomainConfigurationBase, IIdentityServiceConfiguration, IServiceDispatchHost
{
    /// <inheritdoc/>
    IGenericConfiguration? IServiceDispatchHost.ServiceDispatchBody => Configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityServiceConfiguration"/> class.
    /// </summary>
    public IdentityServiceConfiguration()
    {
    }














}
