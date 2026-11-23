using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.TestDouble;

/// <summary>
/// A connection configuration with one member that <see cref="MockConnectionConfiguration"/> does not bind (FDW-6584): what the schema loader's refusal of a key that
/// no class binds needs, to show that a key is judged against the type its entry's Implementation names, not against one type for every connection.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class StrictKeysProbeConnectionConfiguration : MockConnectionConfiguration
{
    /// <summary>Gets or sets a member that only this connection kind binds.</summary>
    public string? OnlyThisKindBindsThis { get; set; }
}

/// <summary>A connection kind whose configuration type is <see cref="StrictKeysProbeConnectionConfiguration"/>; it builds nothing.</summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(ConnectionTypes), OptionName)]
public sealed class StrictKeysProbeConnectionType : ConnectionTypeBase<IGenericConnection, IStrictKeysProbeConnectionFactory, StrictKeysProbeConnectionConfiguration>
{
    /// <summary>The discriminator this option registers under.</summary>
    public const string OptionName = "StrictKeysProbe";

    /// <summary>Initializes a new instance of the <see cref="StrictKeysProbeConnectionType"/> class.</summary>
    public StrictKeysProbeConnectionType() : base(OptionName, OptionName, "Strict keys probe", "A connection kind with a member the other test kinds do not bind", "Test") { }

    /// <inheritdoc/>
    protected override Fdw.Services.Connections.Abstractions.Authentication.ConnectionAuthentication<StrictKeysProbeConnectionConfiguration> EndpointAuthenticationDeclaration
        => Fdw.Services.Connections.Abstractions.Authentication.ConnectionAuthentication<StrictKeysProbeConnectionConfiguration>.None;

    /// <inheritdoc/>
    protected override Fdw.Results.IGenericResult<Fdw.Services.Abstractions.IImplementationServiceProvider<Fdw.Services.Connections.Abstractions.IGenericConnection, Fdw.Services.Connections.Abstractions.IConnectionImplementationConfiguration>> BuildImplementationProvider(
        IStrictKeysProbeConnectionFactory factory, Fdw.Services.SecretManagers.Abstractions.ISecretManagerProvider secretManagers, Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory)
        => Fdw.Results.GenericResult<Fdw.Services.Abstractions.IImplementationServiceProvider<Fdw.Services.Connections.Abstractions.IGenericConnection, Fdw.Services.Connections.Abstractions.IConnectionImplementationConfiguration>>.Failure(
            Fdw.Services.Connections.Results.ConnectionsResultCodes.ByName("ImplementationProviderNotOffered"),
            Fdw.Results.ResultDetails.Create("connectionType", Name));

    /// <inheritdoc/>
    protected override Fdw.Results.IGenericResult<bool> DecideHostIdentity(StrictKeysProbeConnectionConfiguration configuration)
        => Fdw.Results.GenericResult<bool>.Success(false);

    /// <inheritdoc/>
    protected override Fdw.Results.IGenericResult<Fdw.Services.Connections.Abstractions.Authentication.ConnectionCredentialSlots> DescribeCredentials(StrictKeysProbeConnectionConfiguration configuration)
        => Fdw.Results.GenericResult<Fdw.Services.Connections.Abstractions.Authentication.ConnectionCredentialSlots>.Success(Fdw.Services.Connections.Abstractions.Authentication.ConnectionCredentialSlots.None);

    /// <inheritdoc/>
    protected override Fdw.Results.IGenericResult<bool> DecideReadsStoredSecret(StrictKeysProbeConnectionConfiguration configuration)
        => Fdw.Results.GenericResult<bool>.Success(false);

    /// <inheritdoc/>
    protected override Fdw.Results.IGenericResult BindCredentialReferences(
        StrictKeysProbeConnectionConfiguration configuration,
        string secretManagerName,
        System.Collections.Generic.IReadOnlyDictionary<string, string> secretKeyByField)
        => Fdw.Results.GenericResult.Success();
}
