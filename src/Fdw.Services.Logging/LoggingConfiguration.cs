using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Logging.Abstractions;

namespace Fdw.Services.Logging;

/// <summary>
/// The logging domain configuration: which logging implementation is configured, and its settings.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Logging")]
public partial class LoggingConfiguration : ILoggingConfiguration
{
    // Why no generated default: the store assigns identity. A value minted here reaches Get(id) as a
    // real-looking id matching no row, and the miss reads as a data problem rather than an unsaved record.
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section this domain reads.</summary>
    public string SectionName => "Logging";

    /// <summary>Gets the service category this configuration belongs to.</summary>
    public string ServiceType => "Logging";

    /// <summary>Gets or sets the option name selecting which logging implementation is configured.</summary>
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the configuration of the implementation named by <see cref="ServiceOptionType"/>.</summary>
    public ILoggingImplementationConfiguration? Configuration { get; set; }
}
