using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace Fdw.Services.Hosts;

/// <summary>
/// The ForwardedHeaders domain configuration: which ForwardedHeaders implementation is configured, and its settings.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ForwardedHeaders")]
public partial class ForwardedHeadersConfiguration : DomainConfigurationBase, IForwardedHeadersConfiguration
{





}
