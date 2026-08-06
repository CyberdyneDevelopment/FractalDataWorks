using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.SecretManagers.Commands;

/// <summary>
/// Command for retrieving a certificate from a secret provider.
/// Returns the certificate as binary data (PFX format) in a SecretValue.
/// </summary>
/// <remarks>
/// <para>
/// This command retrieves certificates from providers that support certificate storage
/// (e.g., Azure Key Vault Certificates). The certificate is returned as a <see cref="SecretValue"/>
/// with <see cref="SecretValue.IsBinary"/> set to true.
/// </para>
/// <para>
/// Use <see cref="SecretValue.GetBinaryValue"/> or <see cref="SecretValue.AccessBinaryValue{TResult}"/>
/// to access the certificate bytes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var command = GetCertificateManagerCommand.Latest("my-vault", "my-cert", includePrivateKey: true);
/// var result = await secretManager.Execute(command, cancellationToken);
///
/// if (result.IsSuccess)
/// {
///     var certBytes = result.Value.GetBinaryValue();
///     var x509 = new X509Certificate2(certBytes);
/// }
/// </code>
/// </example>
public sealed class GetCertificateManagerCommand : SecretManagerCommandBase, ISecretManagerCommand<SecretValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCertificateManagerCommand"/> class.
    /// </summary>
    /// <param name="container">The certificate container or vault name.</param>
    /// <param name="certificateName">The certificate name or identifier.</param>
    /// <param name="parameters">Command parameters (e.g., Version, IncludePrivateKey).</param>
    /// <param name="metadata">Additional command metadata.</param>
    /// <param name="timeout">Command timeout.</param>
    /// <exception cref="ArgumentException">Thrown when certificateName is null or empty.</exception>
    public GetCertificateManagerCommand(
        string? container,
        string certificateName,
        IReadOnlyDictionary<string, object?>? parameters = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        TimeSpan? timeout = null)
        : base("GetCertificate", container, certificateName, typeof(SecretValue), parameters, metadata, timeout)
    {
        if (string.IsNullOrWhiteSpace(certificateName))
            throw new ArgumentException("Certificate name cannot be null or empty for GetCertificate operation.", nameof(certificateName));
    }

    /// <inheritdoc/>
    public override bool IsSecretModifying => false;

    /// <summary>
    /// Gets the version of the certificate to retrieve.
    /// </summary>
    /// <value>The version identifier, or null to get the latest version.</value>
    public string? Version => Parameters.TryGetValue(nameof(Version), out var version) ? version?.ToString() : null;

    /// <summary>
    /// Gets a value indicating whether to include the private key in the certificate export.
    /// </summary>
    /// <value><c>true</c> to include the private key; otherwise, <c>false</c>.</value>
    public bool IncludePrivateKey => Parameters.TryGetValue(nameof(IncludePrivateKey), out var include) &&
                                     include is bool includeKey && includeKey;

    /// <summary>
    /// Creates a GetCertificateManagerCommand for the latest version of a certificate.
    /// </summary>
    /// <param name="container">The certificate container or vault name.</param>
    /// <param name="certificateName">The certificate name or identifier.</param>
    /// <param name="includePrivateKey">Whether to include the private key.</param>
    /// <param name="timeout">Command timeout.</param>
    /// <returns>A new GetCertificateManagerCommand instance.</returns>
    public static GetCertificateManagerCommand Latest(
        string? container,
        string certificateName,
        bool includePrivateKey = true,
        TimeSpan? timeout = null)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(IncludePrivateKey)] = includePrivateKey
        };

        return new GetCertificateManagerCommand(container, certificateName, parameters, null, timeout);
    }

    /// <summary>
    /// Creates a GetCertificateManagerCommand for a specific version of a certificate.
    /// </summary>
    /// <param name="container">The certificate container or vault name.</param>
    /// <param name="certificateName">The certificate name or identifier.</param>
    /// <param name="version">The version identifier.</param>
    /// <param name="includePrivateKey">Whether to include the private key.</param>
    /// <param name="timeout">Command timeout.</param>
    /// <returns>A new GetCertificateManagerCommand instance.</returns>
    /// <exception cref="ArgumentException">Thrown when version is null or empty.</exception>
    public static GetCertificateManagerCommand ForVersion(
        string? container,
        string certificateName,
        string version,
        bool includePrivateKey = true,
        TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be null or empty.", nameof(version));

        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(Version)] = version,
            [nameof(IncludePrivateKey)] = includePrivateKey
        };

        return new GetCertificateManagerCommand(container, certificateName, parameters, null, timeout);
    }

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return new GetCertificateManagerCommand(Container, SecretKey!, newParameters, Metadata, Timeout);
    }

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return new GetCertificateManagerCommand(Container, SecretKey!, Parameters, newMetadata, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<SecretValue> ISecretManagerCommand<SecretValue>.WithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return new GetCertificateManagerCommand(Container, SecretKey!, newParameters, Metadata, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<SecretValue> ISecretManagerCommand<SecretValue>.WithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return new GetCertificateManagerCommand(Container, SecretKey!, Parameters, newMetadata, Timeout);
    }
}
