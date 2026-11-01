using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Fdw.Services.Connections.Results;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Tests a connection configuration without saving it: builds a connection of whatever type the request
/// names and asks it to connect. Used by the connection wizard to check connectivity before saving.
/// </summary>
/// <remarks>
/// Type-agnostic. The request's <c>ServiceType</c> picks the connection type, whose
/// <see cref="IConnectionType.EndpointBehavior"/> binds the body exactly as a create would; the
/// configuration's <c>Implementation</c> then selects the factory through <see cref="IConnectionProvider"/>.
/// No connection type is named here, so a type added later can be tested with no change.
/// </remarks>
public abstract class TestConnectionConfigEndpointBase : Endpoint<CreateConnectionRequest, TestConnectionResponse>
{
    private readonly IConnectionProvider _connectionProvider;

    /// <summary>Initializes a new instance of the <see cref="TestConnectionConfigEndpointBase"/> class.</summary>
    /// <param name="logger">The endpoint's logger.</param>
    /// <param name="connectionProvider">Builds a connection from a configuration.</param>
    protected TestConnectionConfigEndpointBase(ILogger logger, IConnectionProvider connectionProvider)
    {
        EndpointLogger = logger;
        _connectionProvider = connectionProvider;
    }

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/connections/test-config");
#if DEVELOP
        AllowAnonymous();
#else
        // Why write and not read: testing builds a live connection from caller-supplied settings and
        // probes a network endpoint with them -- the same privilege as creating a connection.
        Policies("connections:write");
#endif
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (summary, tags, etc.).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CreateConnectionRequest req, CancellationToken ct)
    {
        ConnectionEndpointLog.TestingConnectionConfig(EndpointLogger, req.Name);

        var tested = await Test(req, ct).ConfigureAwait(false);
        var response = new TestConnectionResponse
        {
            Name = req.Name,
            Success = tested.IsSuccess,
            Message = tested.IsSuccess ? "Connection successful" : Reason(tested),
        };

        if (response.Success)
            ConnectionEndpointLog.ConnectionConfigTestSucceeded(EndpointLogger, req.Name);
        else
            ConnectionEndpointLog.ConnectionConfigTestFailed(EndpointLogger, req.Name, response.Message);

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    private async Task<IGenericResult> Test(CreateConnectionRequest request, CancellationToken ct)
    {
        var connectionType = ConnectionTypes.ByName(request.ServiceType);
        if (ReferenceEquals(connectionType, ConnectionTypes.NotFound))
            return GenericResult.Failure(ConnectionEndpointLog.ConnectionTypeNotFound(EndpointLogger, request.ServiceType));

        var bound = connectionType.EndpointBehavior.Bind(ConnectionRequestBody.ForCreate(request), EndpointLogger);
        if (bound.IsFailure)
            return bound;

        // Why stamp these: nothing is saved, so no save stamps them, and Implementation is what selects
        // the factory that builds the connection.
        var configuration = bound.Value!;
        configuration.Name = request.Name;
        configuration.Implementation = connectionType.Name;

        // Testing opens the connection, so a connection that acts as the host would sign in as the host to whatever
        // the body names. The same rule as saving one applies (FDW-1068).
        var admitted = HostIdentityGate.Admit(
            connectionType, configuration, Resolve<ISystemRoleConfiguration>().IsPlatformAdmin(User), EndpointLogger);
        if (admitted.IsFailure)
            return admitted;

        // An unsaved connection has no stored credentials, and its body may not name one unless the caller is a platform
        // administrator: a caller who could name a secret could test-open a connection with any secret the platform holds
        // (FDW-1086, FDW-1327).
        if (request.Credentials is { Count: > 0 })
        {
            return GenericResult.Failure(
                ConnectionsResultCodes.ByName("CredentialTestNotSupported"), ResultDetails.Create("name", request.Name));
        }

        var references = ConnectionCredentialStore.RefuseCallerReferences(
            connectionType, before: null, configuration, Resolve<ISystemRoleConfiguration>().IsPlatformAdmin(User), EndpointLogger);
        if (references.IsFailure)
            return references;

        var built = await _connectionProvider.Get(configuration, ct).ConfigureAwait(false);
        if (built.IsFailure)
            return built;

        using var connection = built.Value!;
        return await connection.TestConnection(ct).ConfigureAwait(false);
    }

    // The failure's own messages, most recent last: every failure here carries at least one.
    private static string Reason(IGenericResult result) =>
        string.Join("; ", result.Messages.Select(message => message.Message));
}
