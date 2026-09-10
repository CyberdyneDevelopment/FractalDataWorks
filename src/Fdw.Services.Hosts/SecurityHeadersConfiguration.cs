using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace Fdw.Services.Hosts;

/// <summary>
/// The SecurityHeaders domain configuration: which SecurityHeaders implementation is configured, and its settings.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "SecurityHeaders")]
public partial class SecurityHeadersConfiguration : DomainConfigurationBase, ISecurityHeadersConfiguration
{





}
