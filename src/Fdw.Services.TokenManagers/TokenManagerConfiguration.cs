using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Header configuration for token manager services representing the <c>auth.TokenManager</c> parent
/// table. Fields mirror the current <c>AuthenticationServiceConfiguration</c> header set, plus a
/// tenant/visibility/audit block (see <see cref="TenantId"/> through <see cref="ModifyOnBehalfOf"/>).
/// </summary>
/// <remarks>
/// After loading a header row, <c>TokenManagerConfigurationProvider</c> dispatches to the typed-body
/// provider and sets <see cref="Configuration"/>. Callers read typed fields by casting, e.g.
/// <c>(header.Configuration as OpenIddictTokenManagerConfiguration)</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "TokenManager")]
public partial class TokenManagerConfiguration : DomainConfigurationBase, ITokenManagerConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenManagerConfiguration"/> class.
    /// </summary>
    public TokenManagerConfiguration()
    {
    }
















}
