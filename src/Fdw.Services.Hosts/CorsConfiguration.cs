using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace Fdw.Services.Hosts;

/// <summary>
/// The CORS domain configuration: which CORS implementation is configured, and its settings.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Cors")]
public partial class CorsConfiguration : DomainConfigurationBase, ICorsConfiguration
{






}
