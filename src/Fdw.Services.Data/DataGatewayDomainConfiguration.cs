using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// The data gateway domain record: which implementation, under what name.
/// </summary>
/// <remarks>
/// Identity only. Everything an implementation reads at runtime lives on its own implementation,
/// reached through <see cref="Configuration"/> — a field the factory needs but that sits up here
/// would arrive empty on the implementation configuration and the service would fail to construct.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataGateway")]
public partial class DataGatewayDomainConfiguration : IDataGatewayConfiguration
{
    // Why no generated default: the store assigns identity. A value minted here reaches Get(id) as a
    // real-looking id matching no row, and the miss reads as a data problem rather than an unsaved record.
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this configuration row.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the domain this record belongs to.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets which implementation this record names.</summary>
    public string? Implementation { get; set; }

    /// <summary>Gets or sets the implementation for the implementation this record names.</summary>
    public IDataGatewayImplementationConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// The non-generic view of <see cref="Configuration"/>. The platform service provider reads the
    /// implementation without naming this domain's implementation contract; netstandard2.0 rules out
    /// a default interface implementation, so each domain record states it.
    /// </remarks>
    IImplementationConfiguration? IDomainConfiguration.ImplementationConfiguration
    {
        get => Configuration;
        set => Configuration = (IDataGatewayImplementationConfiguration?)value;
    }

}
