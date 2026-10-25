using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.SignIns;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.SignIns;

/// <summary>
/// Keeps the log of sign-ins in <c>auth.UserSignIn</c> (AuthDb) and reads the latest of each person from the view
/// <c>auth.UserLastSignIn</c> (FDW-2729).
/// </summary>
/// <remarks>
/// A sign-in is recorded as a new row and nothing is updated, so the user's versioned configuration row is not touched and no cached read of
/// the user is invalidated. Each row also carries who was calling, read from the request in hand (<see cref="ISignInCaller"/>). The calls run as the system principal: a sign-in is recorded before its caller is a principal, and the people who
/// read the last sign-ins are an administrator's, not the person's own.
/// </remarks>
public sealed class DatabaseSignInLog : ISignInLog
{
    private const string PathName = "auth";
    private const string SignInContainer = "UserSignIn";
    private const string LastSignInContainer = "UserLastSignIn";

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly ISignInCaller _caller;
    private readonly ILogger<DatabaseSignInLog> _logger;

    /// <summary>Initializes a new instance of the <see cref="DatabaseSignInLog"/> class.</summary>
    /// <param name="gateways">Holds the gateway for the host-declared AuthDb connection, the only path to the authentication state.</param>
    /// <param name="authContextAccessor">Lets each call run as the system principal.</param>
    /// <param name="caller">Says who is calling, to record beside each sign-in.</param>
    /// <param name="logger">The logger.</param>
    public DatabaseSignInLog(
        IConfigurationGatewayProvider gateways,
        IAuthenticationContextAccessor authContextAccessor,
        ISignInCaller caller,
        ILogger<DatabaseSignInLog>? logger = null)
    {
        _gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _caller = caller ?? throw new ArgumentNullException(nameof(caller));
        _logger = logger ?? NullLogger<DatabaseSignInLog>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Record(
        Guid userId, string? serviceName, string flowName, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var caller = _caller.Current();

        var call = Insert.Into<UserSignInEntity>(SignInContainer)
            .DataStore(AuthenticationStepTypes.StateConnection)
            .Path(PathName)
            .Value(new UserSignInEntity
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                SignedInAt = at,
                ServiceName = serviceName,
                FlowName = flowName,
                IpAddress = caller.IpAddress,
                UserAgent = caller.UserAgent,
                Browser = UserAgentRecognizer.BrowserOf(caller.UserAgent),
                OperatingSystem = UserAgentRecognizer.OperatingSystemOf(caller.UserAgent),
            });

        var written = await _gateways.Execute<int>(AuthenticationStepTypes.StateConnection, call, cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return GenericResult.Failure(SignInRecordLog.Failed(_logger, userId.ToString("D"), written.CurrentMessage));

        SignInRecordLog.Recorded(_logger, userId.ToString("D"), flowName, at.ToString("O"), caller.IpAddress is not null, caller.UserAgent is not null);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyDictionary<Guid, DateTimeOffset>>> LastSignIns(
        IReadOnlyList<Guid> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return GenericResult<IReadOnlyDictionary<Guid, DateTimeOffset>>.Success(new Dictionary<Guid, DateTimeOffset>());

        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var gateway = _gateways.Get(AuthenticationStepTypes.StateConnection);
        if (gateway.IsFailure)
            return GenericResult<IReadOnlyDictionary<Guid, DateTimeOffset>>.Failure(SignInRecordLog.ReadFailed(_logger, userIds.Count, gateway.CurrentMessage));

        var call = Query.From<UserLastSignInEntity>(AuthenticationStepTypes.StateConnection, PathName, LastSignInContainer)
            .Where(row => row.UserId).In(userIds)
            .Build();

        // Why not from the result cache: a cached read is dropped by a write to its own container, and a sign-in writes
        // auth.UserSignIn, not this view, so a cached answer would never be dropped and would show a sign-in that is no longer the
        // latest. The log changes on every sign-in and nothing a cache holds is worth more than being current.
        var read = await gateway.Value!.Execute<IEnumerable<UserLastSignInEntity>>(call.Command, call.Target, false, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
            return GenericResult<IReadOnlyDictionary<Guid, DateTimeOffset>>.Failure(SignInRecordLog.ReadFailed(_logger, userIds.Count, read.CurrentMessage));

        return GenericResult<IReadOnlyDictionary<Guid, DateTimeOffset>>.Success(
            read.Value!.ToDictionary(row => row.UserId, row => row.LastSignInAt));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<SignInRecord>>> SignIns(
        Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var gateway = _gateways.Get(AuthenticationStepTypes.StateConnection);
        if (gateway.IsFailure)
            return GenericResult<IReadOnlyList<SignInRecord>>.Failure(SignInRecordLog.ListFailed(_logger, userId.ToString("D"), gateway.CurrentMessage));

        var call = Query.From<UserSignInEntity>(AuthenticationStepTypes.StateConnection, PathName, SignInContainer)
            .Where(row => row.UserId).Equal(userId)
            .OrderByDescending(row => row.SignedInAt)
            .Paging(skip, take)
            .Build();

        // Why not from the result cache: the same reason as the last sign-ins. A sign-in writes this container and a cached page would
        // go on showing the list as it was before it.
        var read = await gateway.Value!.Execute<IEnumerable<UserSignInEntity>>(call.Command, call.Target, false, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
            return GenericResult<IReadOnlyList<SignInRecord>>.Failure(SignInRecordLog.ListFailed(_logger, userId.ToString("D"), read.CurrentMessage));

        return GenericResult<IReadOnlyList<SignInRecord>>.Success(
            read.Value!
                .Select(row => new SignInRecord
                {
                    SignedInAt = row.SignedInAt,
                    ServiceName = row.ServiceName,
                    FlowName = row.FlowName,
                    IpAddress = row.IpAddress,
                    UserAgent = row.UserAgent,
                    Browser = row.Browser,
                    OperatingSystem = row.OperatingSystem,
                })
                .ToList());
    }
}
