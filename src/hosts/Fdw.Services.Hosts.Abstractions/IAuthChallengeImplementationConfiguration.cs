using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The contract every AuthChallenge implementation's configuration satisfies -- what a host returns when a caller is unauthorized.
/// </summary>
public interface IAuthChallengeImplementationConfiguration : IImplementationConfiguration
{
}
