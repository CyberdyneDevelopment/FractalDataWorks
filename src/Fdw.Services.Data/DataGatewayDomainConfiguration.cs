using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// The data gateway domain record: which implementation, under what name.
/// </summary>
/// <remarks>
/// Identity only. Everything an implementation reads at runtime lives on its own implementation,
/// reached through <see cref="Configuration"/> — a field the factory needs but that sits up here
/// would arrive empty on the implementation configuration and the service would fail to construct.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataGateway")]
public partial class DataGatewayDomainConfiguration : DomainConfigurationBase, IDataGatewayConfiguration
{






}
