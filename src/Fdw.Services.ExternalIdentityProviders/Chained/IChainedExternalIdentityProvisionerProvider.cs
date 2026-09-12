using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes.Logging;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>
/// The ChainedExternalIdentityProvisioner implementation of its domain.
/// </summary>
/// <remarks>
/// A per-option interface for the same reason <c>IChainedExternalIdentityProvisionerFactory</c> has one: each implementation
/// closes its base with its OWN interface, and that is what the domain provider registers against.
/// </remarks>
public interface IChainedExternalIdentityProvisionerProvider
    : IImplementationServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>
{
}
