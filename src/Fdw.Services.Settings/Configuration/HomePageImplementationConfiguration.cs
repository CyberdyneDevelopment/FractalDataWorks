using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Settings.Configuration;

/// <summary>The home page implementation of the HomePage domain: one page of a scope in one state (FDW-2325).</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Settings", ServiceType = "HomePage")]
public sealed partial class HomePageImplementationConfiguration : IHomePageImplementationConfiguration
{
    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <inheritdoc />
    public string ScopeKind { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid ScopeId { get; set; }

    /// <inheritdoc />
    public string State { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Body { get; set; } = string.Empty;
}
