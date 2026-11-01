using System;
using System.Collections.Generic;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Tests.EndpointBehavior;

/// <summary>An Azure Blob-shaped connection configuration with no endpoint code of its own.</summary>
[ExcludeFromCodeCoverage]
public sealed class AzureBlobTestConfiguration : IConnectionImplementationConfiguration
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Implementation { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTimeOffset CreateDate { get; set; }

    public DateTimeOffset ModifyDate { get; set; }
    public string? Environment { get; set; }
    public bool HealthCheckEnabled { get; set; }
    public bool HealthCheckOnStartup { get; set; }
    public int? HealthCheckIntervalSeconds { get; set; }
    public string SchemaDiscovery { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;

    [ConnectionSecret]
    public string ConnectionStringSecretName { get; set; } = string.Empty;

    public IDictionary<string, string?> Metadata { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}
