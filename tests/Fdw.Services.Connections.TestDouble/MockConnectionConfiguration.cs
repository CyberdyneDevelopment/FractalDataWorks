using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.TestDouble;

/// <summary>
/// A connection configuration used only to give the schema-load tests a typed body to
/// resolve.
/// </summary>
/// <remarks>
/// Why a mock rather than a real connection: these tests assert that the schema loader
/// resolves a connection's typed configuration from its ServiceOptionType. Which
/// connection it is does not matter — only that one is registered and its body comes back
/// strongly typed. Every real connection implementation lives in reference-servicetypes,
/// so reaching for one would give an FDW test a dependency on a downstream repo purely to
/// obtain a shape it never inspects.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Connection", ServiceType = "MockConnection")]
public partial class MockConnectionConfiguration : IConnectionImplementationConfiguration
{
    /// <summary>Gets or sets the identifier of this typed body row.</summary>
    /// <remarks>Why no default: the provider mints this before INSERT, as the real ones do.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent connection this body hangs off.</summary>
    public Guid ConnectionId { get; set; }

    /// <summary>Gets or sets the single value the schema-load assertions read back.</summary>
    public string Root { get; set; } = string.Empty;

    // Why explicit: the canonical name lives on the parent ConnectionConfiguration row.
    // A typed body is identified by ConnectionId and never resolved by name.
    string IGenericConfiguration.Name
    {
        get => string.Empty;
        set { }
    }

    string IGenericConfiguration.SectionName => "Connections";

    string IGenericConfiguration.ServiceType => "Connection";

    string? IGenericConfiguration.ServiceOptionType => "MockConnection";
}
