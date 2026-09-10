using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Hosts.Abstractions;

namespace Fdw.Services.Hosts;

/// <summary>
/// The ResponseBuffering domain configuration: which ResponseBuffering implementation is configured, and its settings.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ResponseBuffering")]
public partial class ResponseBufferingConfiguration : DomainConfigurationBase, IResponseBufferingConfiguration
{





}
