using Fdw.Conventions;

namespace Fdw.Services.Connections.Tests.EndpointBehavior;

/// <summary>
/// A configuration with a value the type carries (port, encrypt), a choice only the caller makes (trust) and a value no default touches
/// (host, database), for the create-defaults tests (FDW-3823).
/// </summary>
internal sealed class DefaultsTestConfiguration
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public bool Encrypt { get; set; }

    [CallerChoice]
    public bool TrustServerCertificate { get; set; }

    public string Database { get; set; } = string.Empty;
}
