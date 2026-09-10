using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace Fdw.Services.Hosts;

/// <summary>
/// The AuthChallenge domain configuration: which AuthChallenge implementation is configured, and its settings.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "AuthChallenge")]
public partial class AuthChallengeConfiguration : DomainConfigurationBase, IAuthChallengeConfiguration
{





}
