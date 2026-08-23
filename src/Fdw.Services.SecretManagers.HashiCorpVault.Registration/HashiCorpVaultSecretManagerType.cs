using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.HashiCorpVault.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SecretManagers.HashiCorpVault.Registration;

/// <summary>
/// The HashiCorp Vault secret manager — reads stored secrets from the KV engine, or has Vault issue
/// short-lived database credentials from the database engine.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(SecretManagerTypes), "HashiCorpVault")]
public sealed class HashiCorpVaultSecretManagerType
    : SecretManagerTypeBase<ISecretManager, ISecretManagerServiceFactory<ISecretManager, SecretManagerConfiguration>, SecretManagerConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="HashiCorpVaultSecretManagerType"/> class.</summary>
    public HashiCorpVaultSecretManagerType()
        : base(
            name: "HashiCorpVault",
            sectionName: "SecretManagers:HashiCorpVault",
            displayName: "HashiCorp Vault",
            description: "Reads stored secrets from Vault's KV engine, or has Vault issue short-lived database credentials",
            supportedSecretStores: ["HashiCorpVault"],
            supportedSecretTypes: ["Password", "ConnectionString", "ApiKey", "Certificate"],
            supportsRotation: false,
            supportsVersioning: true,
            supportsSoftDelete: false,
            supportsAccessPolicies: true,
            maxSecretSizeBytes: 1024 * 1024,
            supportsBatchOperations: false,
            supportsExpiration: true,
            supportsTagging: false,
            defaultContainerName: "HashiCorpVaultSecretManager")
    {
        // Why Append and not Registration: Registration ASSIGNS, discarding whatever body was already
        // installed — including a segment a base constructor prepended. ConnectionTypeBase prepends its
        // factory registration that way, and six connection kinds silently stopped being creatable when
        // their options used Registration (af522f014). This base prepends nothing today, so either is
        // correct right now; Append stays correct if that ever changes.
        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<HashiCorpVaultSecretManagerType>()
                ?? NullLogger<HashiCorpVaultSecretManagerType>.Instance;

            // Why the option registers its own factory: this is the registry the domain provider reads
            // to turn a configuration's ServiceOptionType into something that can build the service.
            // An option that skips it resolves to "No registered service type matches
            // ServiceOptionType" at the first secret read, which reads like a configuration fault.
            DefaultServiceProvider<ISecretManager, SecretManagerConfiguration, ISecretManagerServiceFactory<ISecretManager, SecretManagerConfiguration>, IServiceConfigurationProvider<SecretManagerConfiguration>>
                .Register(Name, sp => new HashiCorpVaultSecretManagerFactory(
                    sp.GetService<ILoggerFactory>(),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(VaultHttpClient.Name),
                    sp.GetRequiredService<Lazy<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>>()));

            // Why registered here: the factory takes its sibling-provider as a Lazy so resolution
            // happens after the container is built, and nothing else registers that closed Lazy.
            builder.Services.TryAddScoped(sp => new Lazy<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>(
                sp.GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>));

            VaultHttpClient.Register(builder.Services);

            VaultLog.SecretManagerRegistered(log, Name);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
