using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>Reads which role names carry system authority.</summary>
/// <remarks>
/// Closed over the RoleMapping domain's implementation contract, because that is what the domain
/// provider's registry accepts: <c>Register&lt;T&gt;</c> constrains T to
/// <c>IImplementationConfigurationProvider</c> of the domain contract, so a provider closed over
/// anything narrower cannot be registered.
/// </remarks>
public class SystemRoleMappingConfigurationProvider
    : ImplementationConfigurationProviderBase<IRoleMappingImplementationConfiguration>
{

}
