using System;
using System.IO;
using System.Runtime.CompilerServices;
using Fdw.Services.SecretProtection.Abstractions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fdw.Services.SecretProtection.DataProtection;

/// <summary>
/// Installs the Data Protection-backed <see cref="ISecretProtector"/> as this deployment's
/// registration -- a package reference to this assembly IS the registration intent, matching the
/// <c>[TypeOption]</c> convention used everywhere else in this framework.
/// </summary>
internal static class SecretProtectionDataProtectionModuleInitializer
{
    /// <summary>Runs when this assembly loads, before <c>Main</c>.</summary>
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [ModuleInitializer]
    internal static void Register()
#pragma warning restore CA2255
        => SecretProtectorTypes.SetImplementation(static (services, loggerFactory) =>
        {
            if (string.IsNullOrWhiteSpace(SecretProtectorOptions.ApplicationName))
                throw new InvalidOperationException(
                    $"{nameof(SecretProtectorOptions)}.{nameof(SecretProtectorOptions.ApplicationName)} must be set before Register runs.");
            if (string.IsNullOrWhiteSpace(SecretProtectorOptions.KeyRingPath))
                throw new InvalidOperationException(
                    $"{nameof(SecretProtectorOptions)}.{nameof(SecretProtectorOptions.KeyRingPath)} must be set before Register runs.");

            var dataProtectionBuilder = services.AddDataProtection()
                .SetApplicationName(SecretProtectorOptions.ApplicationName)
                .PersistKeysToFileSystem(new DirectoryInfo(SecretProtectorOptions.KeyRingPath));

            if (SecretProtectorOptions.KeyEncryptionCertificate is not null)
                dataProtectionBuilder.ProtectKeysWithCertificate(SecretProtectorOptions.KeyEncryptionCertificate);

            services.TryAddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        });
}
