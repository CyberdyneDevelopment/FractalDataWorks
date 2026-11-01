using System;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Tests.EndpointBehavior;

/// <summary>An Sftp-shaped connection configuration with no endpoint code of its own.</summary>
[ExcludeFromCodeCoverage]
public sealed class SftpTestConfiguration : IConnectionImplementationConfiguration
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

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string Username { get; set; } = string.Empty;
    public string SecretManagerName { get; set; } = string.Empty;

    [ConnectionSecret]
    public string SecretKeyName { get; set; } = string.Empty;
}
