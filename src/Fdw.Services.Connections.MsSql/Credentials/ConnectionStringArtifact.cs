using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.MsSql;

/// <summary>
/// Credential artifact containing a database connection string.
/// </summary>
/// <remarks>
/// <para>
/// This artifact is produced by <c>MsSqlConnectionFactory</c>
/// using the MsSqlAuthenticationTypes TypeCollection for authentication dispatch.
/// </para>
/// <para>
/// The connection string contains all necessary parameters including
/// server, database, authentication credentials, and connection options.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Created by authentication type in factory
/// var auth = MsSqlAuthenticationTypes.ByName(config.AuthenticationType);
/// var fragment = auth.BuildAuthFragment(config.AdditionalProperties, resolvedPassword);
/// var builder = BuildBaseConnectionString(config);
/// builder.Append(fragment.Value);
///
/// // Connection string finalized in factory
/// var connectionString = builder.ToString().TrimEnd(';');
/// var artifact = new ConnectionStringArtifact(connectionString);
/// </code>
/// </example>
[ExcludeFromCodeCoverage]
public sealed class ConnectionStringArtifact : CredentialArtifactBase
{
    /// <summary>
    /// The artifact type name.
    /// </summary>
    public const string TypeName = "ConnectionString";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionStringArtifact"/> class.
    /// </summary>
    /// <param name="connectionString">The complete connection string including credentials.</param>
    /// <exception cref="ArgumentNullException">Thrown when connectionString is null.</exception>
    /// <exception cref="ArgumentException">Thrown when connectionString is empty or whitespace.</exception>
    public ConnectionStringArtifact(string connectionString)
    {
        if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty or whitespace.", nameof(connectionString));

        ConnectionString = connectionString;
    }

    /// <inheritdoc/>
    public override string ArtifactType => TypeName;

    /// <summary>
    /// Gets the complete connection string including credentials.
    /// </summary>
    /// <remarks>
    /// This string may contain sensitive information such as passwords.
    /// Do not log or expose this value.
    /// </remarks>
    public string ConnectionString { get; }

    /// <summary>
    /// Gets a sanitized version of the connection string for logging.
    /// </summary>
    /// <returns>Connection string with password masked.</returns>
    public string GetSanitizedConnectionString()
    {
        // Simple masking - replace password value with ***
        var sanitized = ConnectionString;

        // Mask Password=value; pattern
        var passwordIndex = sanitized.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
        if (passwordIndex >= 0)
        {
            var endIndex = sanitized.IndexOf(';', passwordIndex);
            if (endIndex < 0)
                endIndex = sanitized.Length;

            sanitized = string.Concat(
                sanitized.AsSpan(0, passwordIndex),
                "Password=***",
                sanitized.AsSpan(endIndex));
        }

        return sanitized;
    }
}
