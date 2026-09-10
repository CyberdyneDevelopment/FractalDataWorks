using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace Fdw.Services.Hosts;

/// <summary>
/// The hosting domain configuration: which hosting implementation is configured, and its settings.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Host")]
public partial class HostConfiguration : IHostConfiguration
{
    // Why no generated default: the store assigns identity. A value minted here reaches Get(id) as a
    // real-looking id matching no row, and the miss reads as a data problem rather than an unsaved record.
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the domain this record belongs to.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the option name selecting which hosting implementation is configured.</summary>
    public string? Implementation { get; set; }

    /// <summary>Gets or sets the human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the configuration of the implementation named by <see cref="Implementation"/>.</summary>
    public IHostImplementationConfiguration? Configuration { get; set; }

    // Why these five live on the domain header rather than a typed body of their own: a typed body
    // is chosen by Implementation and a host runs several options at once (Cors AND SecurityHeaders
    // AND EmptyBody...), so "the implementation" for a Host row is never singular the way it is for a
    // Connection. The support contact is not an implementation choice at all -- it describes the host
    // itself, the same way Description already does.
    /// <summary>Gets or sets the email address shown to a caller when a request fails.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the phone number shown to a caller when a request fails.</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets the support portal URL shown to a caller when a request fails.</summary>
    public string? PortalUrl { get; set; }

    /// <summary>Gets or sets the expected response time, in hours, quoted to a caller when a request fails.</summary>
    public int ExpectedResponseTimeHours { get; set; }

    /// <summary>Gets or sets the instructions shown to a caller when a request fails.</summary>
    public string Instructions { get; set; } = string.Empty;

    /// <inheritdoc />
    /// <remarks>
    /// The non-generic view of <see cref="Configuration"/>. The platform service provider reads the
    /// implementation without naming this domain's implementation contract; netstandard2.0 rules out
    /// a default interface implementation, so each domain record states it.
    /// </remarks>
    IImplementationConfiguration? IDomainConfiguration.ImplementationConfiguration
    {
        get => Configuration;
        set => Configuration = (IHostImplementationConfiguration?)value;
    }

}
