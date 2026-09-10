using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace Fdw.Services.Hosts;

/// <summary>
/// The ResponseBuffering domain configuration: which ResponseBuffering implementation is configured, and its settings.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ResponseBuffering")]
public partial class ResponseBufferingConfiguration : IResponseBufferingConfiguration
{
    // Why no generated default: the store assigns identity. A value minted here reaches Get(id) as a
    // real-looking id matching no row, and the miss reads as a data problem rather than an unsaved record.
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the domain this record belongs to.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the option name selecting which ResponseBuffering implementation is configured.</summary>
    public string? Implementation { get; set; }

    /// <summary>Gets or sets the human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the configuration of the implementation named by <see cref="Implementation"/>.</summary>
    public IResponseBufferingImplementationConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    IImplementationConfiguration? IDomainConfiguration.ImplementationConfiguration
    {
        get => Configuration;
        set => Configuration = (IResponseBufferingImplementationConfiguration?)value;
    }
}
