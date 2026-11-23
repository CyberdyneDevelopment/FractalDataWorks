using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Authentication;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.TestDouble;

/// <summary>
/// A test connection kind that has one secret credential, so the start-up checks of FDW-6360 have a kind to read a secret slot from. The reference
/// to the secret is carried in the configuration's <c>Description</c> as <c>manager|key</c>; either part may be empty. It builds no provider.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(ConnectionTypes), OptionName)]
public sealed class SecretProbeConnectionType
    : ConnectionTypeBase<IGenericConnection, IMockConnectionFactory, MockConnectionConfiguration>
{
    /// <summary>The discriminator this option registers under.</summary>
    public const string OptionName = "SecretProbe";

    /// <summary>Initializes a new instance of the <see cref="SecretProbeConnectionType"/> class.</summary>
    public SecretProbeConnectionType()
        : base(
            name: OptionName,
            sectionName: OptionName,
            displayName: "Secret probe (test)",
            description: "Test-owned connection that needs one secret, named by its Description as manager|key.",
            category: "Test")
    {
    }

    /// <summary>Writes the reference a probe connection carries.</summary>
    /// <param name="manager">The manager's name, or empty for none.</param>
    /// <param name="key">The secret key, or empty for none.</param>
    /// <returns>The text to put in <c>Description</c>.</returns>
    public static string Reference(string manager, string key) => string.Concat(manager, "|", key);

    /// <inheritdoc/>
    protected override ConnectionAuthentication<MockConnectionConfiguration> EndpointAuthenticationDeclaration
        => ConnectionAuthentication<MockConnectionConfiguration>.None;

    /// <inheritdoc/>
    protected override IGenericResult<bool> DecideHostIdentity(MockConnectionConfiguration configuration)
        => GenericResult<bool>.Success(false);

    /// <inheritdoc/>
    protected override IGenericResult<ConnectionCredentialSlots> DescribeCredentials(MockConnectionConfiguration configuration)
    {
        // A probe connection that carries no reference names no authentication method: like a real kind, it cannot say which credentials
        // it uses, and refuses with CredentialsUndetermined (the Connections-20004 of a connection saved or read with no method).
        if (string.IsNullOrEmpty(configuration.Description))
        {
            return GenericResult<ConnectionCredentialSlots>.Failure(
                Results.ConnectionsResultCodes.ByName("CredentialsUndetermined"),
                ResultDetails.Create("name", configuration.Name, "reason", "it names no authentication method"));
        }

        var parts = (configuration.Description ?? string.Empty).Split('|');
        var values = new Dictionary<string, string?>
        {
            ["SecretKeyName"] = parts.Length > 1 ? parts[1] : string.Empty,
            [ConnectionCredentialProperties.SecretManager] = parts[0],
        };
        return GenericResult<ConnectionCredentialSlots>.Success(
            ConnectionCredentialSlots.From("Probe", [new ConnectionCredentialField("SecretKeyName", "Password", secret: true)], values));
    }

    /// <inheritdoc/>
    /// <remarks>A probe connection that carries a reference names the method <c>Probe</c>, whose credential is a secret; one that carries none names no method and reads no secret.</remarks>
    protected override IGenericResult<bool> DecideReadsStoredSecret(MockConnectionConfiguration configuration)
        => GenericResult<bool>.Success(!string.IsNullOrEmpty(configuration.Description));

    /// <inheritdoc/>
    protected override IGenericResult BindCredentialReferences(
        MockConnectionConfiguration configuration, string secretManagerName, IReadOnlyDictionary<string, string> secretKeyByField)
        => GenericResult.Success();

    /// <inheritdoc/>
    protected override IGenericResult<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>> BuildImplementationProvider(
        IMockConnectionFactory factory, ISecretManagerProvider secretManagers, ILoggerFactory? loggerFactory)
        => GenericResult<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>>.Failure(
            Results.ConnectionsResultCodes.ByName("ImplementationProviderNotOffered"),
            ResultDetails.Create("connectionType", Name));
}
