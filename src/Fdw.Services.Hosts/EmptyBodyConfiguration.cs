using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace Fdw.Services.Hosts;

/// <summary>
/// The EmptyBody domain configuration: which EmptyBody implementation is configured, and its settings.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "EmptyBody")]
public partial class EmptyBodyConfiguration : DomainConfigurationBase, IEmptyBodyConfiguration
{





}
