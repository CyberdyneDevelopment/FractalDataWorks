using Fdw.Configuration;

namespace Fdw.Services.Credentials.Abstractions;

/// <summary>
/// One configured credential service — the domain record, naming which implementation it is and
/// holding that implementation's own configuration.
/// </summary>
public interface ICredentialServiceConfiguration
    : IPlatformServiceConfiguration<ICredentialServiceImplementationConfiguration>
{
}
