using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.TestDouble;

/// <summary>The configuration of <see cref="DdlProbeConnectionType"/>: the mock's, under a type of its own so the kind registers beside it.</summary>
[ExcludeFromCodeCoverage]
public sealed class DdlProbeConnectionConfiguration : MockConnectionConfiguration
{
}

/// <summary>A connection kind that offers DDL execution (FDW-3617) and records what the call is handed; it builds nothing.</summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(ConnectionTypes), OptionName)]
public sealed class DdlProbeConnectionType : ConnectionTypeBase<IGenericConnection, IDdlProbeConnectionFactory, DdlProbeConnectionConfiguration>, IDdlExecution
{
    /// <summary>The discriminator this option registers under.</summary>
    public const string OptionName = "DdlProbe";

    /// <summary>Initializes a new instance of the <see cref="DdlProbeConnectionType"/> class.</summary>
    public DdlProbeConnectionType() : base(OptionName, OptionName, "DDL probe", "A connection kind that offers DDL execution and records what it is handed", "Test") { }

    /// <summary>Gets the connection the last call was handed.</summary>
    public IGenericConnection? ReceivedConnection { get; private set; }

    /// <summary>Gets the commands the last call was handed.</summary>
    public IReadOnlyList<IDdlCommand>? ReceivedCommands { get; private set; }

    /// <summary>Gets the token the last call was handed.</summary>
    public CancellationToken ReceivedToken { get; private set; }

    /// <summary>Gets the number of calls since <see cref="Reset"/>.</summary>
    public int Calls { get; private set; }

    /// <summary>Gets or sets what the next call answers; a test states it, none is assumed.</summary>
    public IGenericResult? Answer { get; set; }

    /// <summary>Forgets what was recorded and the answer, so a test starts from nothing.</summary>
    public void Reset()
    {
        ReceivedConnection = null;
        ReceivedCommands = null;
        ReceivedToken = CancellationToken.None;
        Calls = 0;
        Answer = null;
    }

    /// <inheritdoc/>
    public Task<IGenericResult> ExecuteDdl(IGenericConnection connection, IReadOnlyList<IDdlCommand> commands, CancellationToken cancellationToken = default)
    {
        Calls++;
        ReceivedConnection = connection;
        ReceivedCommands = commands;
        ReceivedToken = cancellationToken;
        return Task.FromResult(Answer ?? throw new System.InvalidOperationException("A test states the probe's answer before it calls."));
    }

    /// <inheritdoc/>
    protected override Fdw.Services.Connections.Abstractions.Authentication.ConnectionAuthentication<DdlProbeConnectionConfiguration> EndpointAuthenticationDeclaration
        => Fdw.Services.Connections.Abstractions.Authentication.ConnectionAuthentication<DdlProbeConnectionConfiguration>.None;

    /// <inheritdoc/>
    protected override Fdw.Results.IGenericResult<Fdw.Services.Abstractions.IImplementationServiceProvider<Fdw.Services.Connections.Abstractions.IGenericConnection, Fdw.Services.Connections.Abstractions.IConnectionImplementationConfiguration>> BuildImplementationProvider(
        IDdlProbeConnectionFactory factory, Fdw.Services.SecretManagers.Abstractions.ISecretManagerProvider secretManagers, Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory)
        => Fdw.Results.GenericResult<Fdw.Services.Abstractions.IImplementationServiceProvider<Fdw.Services.Connections.Abstractions.IGenericConnection, Fdw.Services.Connections.Abstractions.IConnectionImplementationConfiguration>>.Failure(
            Fdw.Services.Connections.Results.ConnectionsResultCodes.ByName("ImplementationProviderNotOffered"),
            Fdw.Results.ResultDetails.Create("connectionType", Name));

    /// <inheritdoc/>
    protected override Fdw.Results.IGenericResult<bool> DecideHostIdentity(DdlProbeConnectionConfiguration configuration)
        => Fdw.Results.GenericResult<bool>.Success(false);

    /// <inheritdoc/>
    protected override Fdw.Results.IGenericResult<Fdw.Services.Connections.Abstractions.Authentication.ConnectionCredentialSlots> DescribeCredentials(DdlProbeConnectionConfiguration configuration)
        => Fdw.Results.GenericResult<Fdw.Services.Connections.Abstractions.Authentication.ConnectionCredentialSlots>.Success(Fdw.Services.Connections.Abstractions.Authentication.ConnectionCredentialSlots.None);

    /// <inheritdoc/>
    protected override Fdw.Results.IGenericResult<bool> DecideReadsStoredSecret(DdlProbeConnectionConfiguration configuration)
        => Fdw.Results.GenericResult<bool>.Success(false);

    /// <inheritdoc/>
    protected override Fdw.Results.IGenericResult BindCredentialReferences(
        DdlProbeConnectionConfiguration configuration,
        string secretManagerName,
        System.Collections.Generic.IReadOnlyDictionary<string, string> secretKeyByField)
        => Fdw.Results.GenericResult.Success();
}
