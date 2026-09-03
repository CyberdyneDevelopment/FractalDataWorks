using System.Security.Cryptography.X509Certificates;

namespace Fdw.Services.SecretProtection.DataProtection;

/// <summary>
/// Configuration for <see cref="DataProtectionSecretProtector"/>, set by the host before
/// <c>SecretProtectorTypes.Register</c> runs.
/// </summary>
/// <remarks>
/// No property here has a default. An unset <see cref="ApplicationName"/> or <see cref="KeyRingPath"/>
/// means Data Protection would fall back to its own per-machine local key ring, which silently breaks
/// the exact multi-instance case <c>DatabaseExecutionStore</c> exists for -- so both are required, and
/// the module initializer that reads them fails loud rather than falling back to that default.
/// </remarks>
public static class SecretProtectorOptions
{
    /// <summary>Gets or sets the Data Protection application name/discriminator. Required.</summary>
    public static string? ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the key-ring persistence directory. Required, and must be a location shared
    /// across every instance that needs to decrypt the same ciphertext -- a local/per-machine path
    /// defeats the point as surely as leaving this unset would.
    /// </summary>
    public static string? KeyRingPath { get; set; }

    /// <summary>
    /// Gets or sets an optional certificate to encrypt the key ring at rest. Without one, keys are
    /// stored in clear text under whatever protects <see cref="KeyRingPath"/> itself (filesystem
    /// ACLs) -- acceptable for many deployments, not a substitute for a KMS.
    /// </summary>
    public static X509Certificate2? KeyEncryptionCertificate { get; set; }
}
