using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace Fdw.Services.Hosts.Configuration;

/// <summary>
/// What a host IS, beyond the four columns that identify it: the support contact a surface names
/// when it fails, and whether the host is enabled.
/// </summary>
/// <remarks>
/// Why Host has a single implementation: the features a host runs -- Cors, SecurityHeaders,
/// ApiReference, AuthChallenge -- are each their own domain with their own domain/implementation
/// pair, and a host runs several at once, so none of them is "the" Host implementation. That leaves
/// Host a single-implementation domain, which takes the <c>[Domain]Implementation</c> name.
/// <para>
/// The settings are declared here rather than on <see cref="IHostImplementationConfiguration"/> so
/// that adding a host option never widens a shared interface.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Host", ServiceType = "Host")]
public sealed partial class HostImplementationConfiguration : IHostImplementationConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain record's durable id.</summary>
    public Guid HostId { get; set; }

    /// <summary>Gets or sets the domain record's row id -- the foreign key the constraint is on.</summary>
    public int HostRowId { get; set; }

    /// <summary>Gets or sets whether this host is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the support email address this host names when it fails.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the support phone number, when the host publishes one.</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets the support portal URL, when the host publishes one.</summary>
    public string? PortalUrl { get; set; }

    /// <summary>Gets or sets the response time the host commits to, in hours. Zero means no commitment.</summary>
    public int ExpectedResponseTimeHours { get; set; }

    /// <summary>Gets or sets what the host tells a caller to do when it fails.</summary>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }
}
