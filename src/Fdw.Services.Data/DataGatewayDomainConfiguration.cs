using System;
using Fdw.Configuration;
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
public sealed class DataGatewayDomainConfiguration : IDataGatewayConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the name of this configuration row.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section this record belongs to.</summary>
    public string SectionName => "DataGateway";

    /// <summary>Gets the service type this record configures.</summary>
    public string ServiceType => "DataGateway";

    /// <summary>Gets or sets which implementation this record names.</summary>
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the implementation for the implementation this record names.</summary>
    public IDataGatewayImplementationConfiguration? Configuration { get; set; }
}
