using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>The contract every ExternalIdentityProvisionerBinding implementation carries.</summary>
public interface IExternalIdentityProvisionerBindingImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid ExternalIdentityProvisionerBindingId { get; set; }

    /// <summary>Gets or sets the ExternalIdentityProvisionerBinding TenantId.</summary>
    Guid? TenantId { get; set; }

    /// <summary>Gets or sets the ExternalIdentityProvisionerBinding ProviderName.</summary>
    string ProviderName { get; set; }

    /// <summary>Gets or sets the ExternalIdentityProvisionerBinding ProvisionerName.</summary>
    string ProvisionerName { get; set; }
}
