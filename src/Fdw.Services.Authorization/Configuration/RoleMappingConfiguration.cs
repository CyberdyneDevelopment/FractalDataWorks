using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Authorization.Abstractions;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>
/// The role-mapping domain configuration: which role-mapping implementation is configured, and its settings.
/// </summary>
/// <remarks>
/// A role mapping answers which role names carry which authority. The System implementation names the
/// roles carrying system authority; another implementation could describe a different authority scope,
/// which is why the mapping is a domain rather than a single record.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "RoleMapping")]
public partial class RoleMappingConfiguration : DomainConfigurationBase, IRoleMappingConfiguration
{






}
