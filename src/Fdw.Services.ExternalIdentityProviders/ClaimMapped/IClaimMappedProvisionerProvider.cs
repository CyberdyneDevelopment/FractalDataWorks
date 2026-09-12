using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes.Logging;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Fdw.Services.Users;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.ExternalIdentityProviders.ClaimMapped;

/// <summary>
/// The ClaimMappedProvisioner implementation of its domain.
/// </summary>
/// <remarks>
/// A per-option interface for the same reason <c>IClaimMappedProvisionerFactory</c> has one: each implementation
/// closes its base with its OWN interface, and that is what the domain provider registers against.
/// </remarks>
public interface IClaimMappedProvisionerProvider
    : IImplementationServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>
{
}
