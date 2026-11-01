using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Tds.Endpoints.Logging;
using Fdw.Tds.Endpoints.Sql;
using Fdw.Tds.Endpoints.Sql.Functions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// Signs a SQL login in: the user id and password run the configured authentication flow, exactly as a
/// password login over HTTP runs it, and the session the flow delivers is what the connection holds.
/// </summary>
/// <remarks>
/// <para>
/// Integrated (Windows) authentication and federated tokens are refused: the vendor-facing door is a SQL
/// login, proved by this platform's own credential store. A client asking to change its password at login
/// is refused too; a password is changed through the platform, not through a SQL connection.
/// </para>
/// <para>
/// Every refusal reaches the client as error 18456 and nothing more, as SQL Server answers a failed login:
/// which of the user, the password or the account was wrong is in the log for an operator, and out of the
/// answer so it cannot be used to find accounts.
/// </para>
/// </remarks>
public sealed class TdsLogin
{
    private readonly TdsEndpointOptions _options;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="TdsLogin"/> class.</summary>
    /// <param name="options">The endpoint's options.</param>
    /// <param name="logger">The logger.</param>
    public TdsLogin(TdsEndpointOptions options, ILogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Signs a login in.</summary>
    /// <param name="login">The client's LOGIN7.</param>
    /// <param name="services">The login's own DI scope.</param>
    /// <param name="sessionId">The SPID the connection was given.</param>
    /// <param name="cancellationToken">A token to cancel the sign-in.</param>
    /// <returns>The session, or the errors the client is refused with.</returns>
    public async Task<TdsLoginOutcome> SignIn(Login7Request login, IServiceProvider services, int sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(login);
        ArgumentNullException.ThrowIfNull(services);

        var refusal = Precondition(login);
        if (refusal is not null)
            return Refused(login, refusal);

        var flow = await Flow(services, cancellationToken).ConfigureAwait(false);
        if (flow is null)
            return Refused(login, "the login flow is not usable");

        var token = await Run(login, flow, services, cancellationToken).ConfigureAwait(false);
        if (token.IsFailure)
            return Refused(login, token.Code is { } code ? code.Code : "the login flow refused the credential");

        // Why after the password: refused first, the database would tell a stranger which names this server holds
        // before they had proved anything. SQL Server proves the login first too.
        var database = Database(login);
        if (database is null)
            return await DatabaseRefused(login, token.Value!, flow.SessionServiceName!, services, sessionId, cancellationToken).ConfigureAwait(false);

        var session = new TdsSession(sessionId, login.UserName, login.ApplicationName, login.HostName,
            login.ClientInterfaceName, token.Value!, flow.SessionServiceName!, database);
        TdsEndpointLog.LoginSucceeded(_logger, sessionId, login.UserName, login.ApplicationName, login.ClientInterfaceName);
        return TdsLoginOutcome.SignedIn(session, TdsVersions.Negotiate(login.TdsVersion));
    }

    private static string? Precondition(Login7Request login)
    {
        if (login.UsesIntegratedSecurity || login.Requests(Login7Request.FederatedAuthenticationFeature))
            return "the client asked for integrated or federated authentication; only a SQL login is accepted";

        if (login.ChangesPassword)
            return "the client asked to change its password at login";

        return TdsVersions.Negotiate(login.TdsVersion) == 0 ? $"TDS version 0x{login.TdsVersion:X8} is older than 7.2" : null;
    }

    // No database named opens the one this server serves, as a login's default database does; master opens
    // master; any other database cannot be opened.
    private string? Database(Login7Request login)
    {
        if (login.Database.Length == 0 || string.Equals(login.Database, _options.Identity.DatabaseName, StringComparison.OrdinalIgnoreCase))
            return _options.Identity.DatabaseName;

        return string.Equals(login.Database, Databases.Master, StringComparison.OrdinalIgnoreCase) ? Databases.Master : null;
    }

    private async Task<AuthenticationFlow?> Flow(IServiceProvider services, CancellationToken cancellationToken)
    {
        var flows = services.GetRequiredService<IAuthenticationFlowProvider>();
        var flow = await flows.Get(_options.LoginFlowName, cancellationToken).ConfigureAwait(false);
        if (flow.IsFailure || flow.Value is null)
        {
            TdsEndpointLog.ConfigurationInvalid(_logger, "login flow", $"'{_options.LoginFlowName}' cannot be read");
            return null;
        }

        if (flow.Value.SessionServiceName is null)
        {
            TdsEndpointLog.FlowDeliversNoSession(_logger, _options.LoginFlowName);
            return null;
        }

        return flow.Value;
    }

    // The credential is presented to the flow for exactly this run, as POST /auth/login presents it.
    private async Task<IGenericResult<string>> Run(Login7Request login, AuthenticationFlow flow, IServiceProvider services, CancellationToken cancellationToken)
    {
        var runner = services.GetRequiredService<AuthenticationRunner>();
        var presented = services.GetRequiredService<PresentedPasswordCredentialAccessor>();
        IGenericResult<FlowResult> result;
        using (new PresentedPasswordCredentialScope(presented, login.UserName, login.Password))
        {
            result = await runner.Run(flow, cancellationToken).ConfigureAwait(false);
        }

        if (result.IsFailure)
            return result.ToNewResult<string>();

        return result.Value is FlowResult.Completed { Token.TokenType: SessionRecord.TokenType } completed
            ? GenericResult<string>.Success(completed.Token.AccessToken)
            : GenericResult<string>.Failure(TdsEndpointLog.FlowDeliversNoSession(_logger, flow.Name));
    }

    // The login proved itself but named a database this server does not hold. The session the flow just delivered is
    // closed again before the refusal, so a refused login leaves nothing behind that opens the platform.
    private async Task<TdsLoginOutcome> DatabaseRefused(
        Login7Request login, string sessionToken, string serviceName, IServiceProvider services, int sessionId, CancellationToken cancellationToken)
    {
        var closed = await TdsSessionClosing.Close(services.GetRequiredService<ISessionStore>(), sessionToken, serviceName, cancellationToken).ConfigureAwait(false);
        if (closed.IsFailure)
            TdsEndpointLog.SessionNotClosed(_logger, sessionId);

        TdsEndpointLog.LoginRefused(_logger, login.UserName, $"database '{login.Database}' is not one this server holds");
        return TdsLoginOutcome.Refused([SqlServerErrors.CannotOpenDatabase(login.Database), SqlServerErrors.LoginFailed(login.UserName)]);
    }

    private TdsLoginOutcome Refused(Login7Request login, string reason)
    {
        TdsEndpointLog.LoginRefused(_logger, login.UserName, reason);
        return TdsLoginOutcome.Refused([SqlServerErrors.LoginFailed(login.UserName)]);
    }
}
