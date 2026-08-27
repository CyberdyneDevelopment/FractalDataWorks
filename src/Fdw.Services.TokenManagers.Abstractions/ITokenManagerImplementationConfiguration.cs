using Fdw.Configuration;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// Marker interface for typed token-manager body configurations (e.g. an OpenIddict-specific
/// configuration carrying Authority/TokenEndpoint/lifetimes). Each typed body implements this
/// interface directly without inheriting from a concrete header class — the header
/// (<c>TokenManagerConfiguration</c>) carries a <c>[NotMapped] ITokenManagerImplementationConfiguration?
/// Configuration</c> property populated on the read path, mirroring every other polymorphic
/// header/typed-body domain (Connection, SecretManager, AuthenticationService).
/// </summary>
public interface ITokenManagerImplementationConfiguration : IImplementationConfiguration
{
}
