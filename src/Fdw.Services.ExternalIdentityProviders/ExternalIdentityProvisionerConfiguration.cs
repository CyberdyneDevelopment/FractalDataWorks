using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// The external-identity provisioning domain, backed by <c>sec.ExternalIdentityProvisioner</c>: it
/// names which provisioning mechanism is configured and holds that implementation's own
/// configuration. <see cref="Domain"/> is stated by the type; <see cref="Implementation"/> is read
/// from the row. A tenant/visibility/audit block follows (see <see cref="TenantId"/> through
/// <see cref="ModifyOnBehalfOf"/>). NO secret column exists here — this domain selects a
/// provisioning mechanism, it is not a credential.
/// </summary>
/// <remarks>
/// After loading a header row, <c>ExternalIdentityProvisionerConfigurationProvider</c> dispatches to
/// the typed-body provider and sets <see cref="Configuration"/>. Callers read typed fields by casting,
/// e.g. <c>(header.Configuration as ChainedExternalIdentityProvisionerConfiguration)</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ExternalIdentityProvisioner")]
public partial class ExternalIdentityProvisionerConfiguration : DomainConfigurationBase, IExternalIdentityProvisionerConfiguration
{














}
