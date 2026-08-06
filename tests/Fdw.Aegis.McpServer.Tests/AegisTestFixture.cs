using System;
using System.Threading.Tasks;
using Fdw.Aegis.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Http;
using Fdw.Services.Data.Configuration;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.TestDouble;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Aegis.McpServer.Tests;

/// <summary>
/// Shared xUnit class fixture: builds two synthetic downstream stubs (one polite, one hostile) + the
/// REAL Aegis DI graph exactly ONCE for the whole <see cref="AegisNonExposureTests"/> class.
/// </summary>
/// <remarks>
/// Why shared, not per-Fact: <c>SecretManagerTypes</c>' generated <c>Register(...)</c> guards each
/// option's <c>Register</c> with a process-static <c>_registeredOptionNames</c> set —
/// by design, a real host calls <c>Register(...)</c> exactly once per process. Building a fresh
/// <see cref="IServiceCollection"/> per test and calling <c>Register(...)</c> against it repeatedly
/// would silently skip registration on every host after the first, so the fixture mirrors the real
/// one-host-per-process shape and each <see cref="Xunit.Fact"/> only creates a fresh DI *scope* off
/// this one host, matching how <c>ModelContextProtocol</c>'s <c>WithTools&lt;T&gt;</c> activates a
/// tool per call in production. Every adversarial case (hostile downstream, header-invalid secret) is
/// therefore expressed as an extra declared connection/command/secret on this one host, not a second
/// host.
/// </remarks>
public sealed class AegisTestFixture : IAsyncLifetime
{
    private const string TokenEnvironmentVariable = "FDW_SECRET_AEGIS_SYNTHETIC_TOKEN";
    private const string BadCharTokenEnvironmentVariable = "FDW_SECRET_AEGIS_BADCHAR_TOKEN";

    /// <summary>Gets the polite synthetic downstream stub (returns only a fingerprint).</summary>
    public SyntheticEchoStub Stub { get; private set; } = null!;

    /// <summary>Gets the hostile downstream stub (echoes the received credential in its body).</summary>
    public SyntheticEchoStub HostileStub { get; private set; } = null!;

    /// <summary>Gets the log line collector attached to the host's logging pipeline.</summary>
    public ListLoggerProvider LogCollector { get; private set; } = null!;

    /// <summary>Gets the built host — one per fixture instance, per process.</summary>
    public IHost Host { get; private set; } = null!;

    /// <summary>Gets the random per-run token backing <c>FDW_SECRET_AEGIS_SYNTHETIC_TOKEN</c>.</summary>
    public string Token { get; private set; } = null!;

    /// <summary>
    /// Gets a random per-run secret that is INVALID as an HTTP header value (contains a newline),
    /// backing <c>FDW_SECRET_AEGIS_BADCHAR_TOKEN</c> — used to prove the injector rejects it before
    /// building a header and never surfaces it.
    /// </summary>
    public string BadCharToken { get; private set; } = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        Stub = await SyntheticEchoStub.Start(hostile: false).ConfigureAwait(false);
        HostileStub = await SyntheticEchoStub.Start(hostile: true).ConfigureAwait(false);

        // Why: fresh random secrets per test run — never fixed literals — so a passing assertion
        // can't be explained by coincidence. BadCharToken embeds a newline, making it invalid as an
        // HTTP header value.
        Token = Guid.NewGuid().ToString("N");
        BadCharToken = $"bad\n{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(TokenEnvironmentVariable, Token);
        Environment.SetEnvironmentVariable(BadCharTokenEnvironmentVariable, BadCharToken);

        LogCollector = new ListLoggerProvider();

        var schema = BuildSchema(Stub.Address, HostileStub.Address);

        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(LogCollector);
        builder.Logging.SetMinimumLevel(LogLevel.Trace);

        AegisHostRegistration.Configure(builder, loggerFactory: null);
        AegisHostRegistration.Register(builder, schema, loggerFactory: null);

        Host = builder.Build();

        AegisHostRegistration.Initialize(Host.Services, schema, loggerFactory: null);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Environment.SetEnvironmentVariable(TokenEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(BadCharTokenEnvironmentVariable, null);
        await Host.StopAsync().ConfigureAwait(false);
        Host.Dispose();
        await Stub.DisposeAsync().ConfigureAwait(false);
        await HostileStub.DisposeAsync().ConfigureAwait(false);
    }

    private static ConfigurationSchema BuildSchema(string stubAddress, string hostileAddress)
    {
        var syntheticConnection = new ConnectionConfiguration
        {
            Name = "synthetic-echo",
            ServiceOptionType = "Http",
            Configuration = new HttpConnectionConfiguration { BaseUrl = stubAddress },
        };

        var hostileConnection = new ConnectionConfiguration
        {
            Name = "hostile-echo",
            ServiceOptionType = "Http",
            Configuration = new HttpConnectionConfiguration { BaseUrl = hostileAddress },
        };

        var secretManager = new SecretManagerConfiguration
        {
            Name = "EnvSecrets",
            ServiceOptionType = SyntheticSecretManagerType.OptionName,
            Configuration = new SyntheticSecretManagerConfiguration { Prefix = "FDW_SECRET_" },
        };

        var preApproved = new AegisCommandConfiguration
        {
            Name = "echo_credential",
            ConnectionName = "synthetic-echo",
            ServiceOptionType = "PreApproved",
            Configuration = new PreApprovedCommandConfiguration
            {
                SecretManagerName = "EnvSecrets",
                SecretKeyName = "AEGIS_SYNTHETIC_TOKEN",
                ParameterAllowList =
                [
                    new ParameterAllowEntry { ParameterName = "mode", PermittedValues = ["echo"], Required = true },
                ],
            },
        };

        var adHoc = new AegisCommandConfiguration
        {
            Name = "echo_adhoc",
            ConnectionName = "synthetic-echo",
            ServiceOptionType = "AdHoc",
            Configuration = new AdHocCommandConfiguration
            {
                SecretManagerName = "EnvSecrets",
                SecretKeyName = "AEGIS_SYNTHETIC_TOKEN",
            },
        };

        // Why: a PreApproved command that succeeds (200) against a downstream that ECHOES the token in
        // its body — the adversarial case proving the gateway surfaces none of the reflected credential.
        var hostile = new AegisCommandConfiguration
        {
            Name = "echo_hostile",
            ConnectionName = "hostile-echo",
            ServiceOptionType = "PreApproved",
            Configuration = new PreApprovedCommandConfiguration
            {
                SecretManagerName = "EnvSecrets",
                SecretKeyName = "AEGIS_SYNTHETIC_TOKEN",
                ParameterAllowList =
                [
                    new ParameterAllowEntry { ParameterName = "mode", PermittedValues = ["echo"], Required = true },
                ],
            },
        };

        // Why: a PreApproved command whose secret is invalid as an HTTP header value — proving the
        // injector rejects it BEFORE any downstream call and never surfaces the value.
        var badChar = new AegisCommandConfiguration
        {
            Name = "echo_badchar",
            ConnectionName = "synthetic-echo",
            ServiceOptionType = "PreApproved",
            Configuration = new PreApprovedCommandConfiguration
            {
                SecretManagerName = "EnvSecrets",
                SecretKeyName = "AEGIS_BADCHAR_TOKEN",
                ParameterAllowList =
                [
                    new ParameterAllowEntry { ParameterName = "mode", PermittedValues = ["echo"], Required = true },
                ],
            },
        };

        return new ConfigurationSchema
        {
            Connections = [syntheticConnection, hostileConnection],
            SecretManagers = [secretManager],
            Commands = [preApproved, adHoc, hostile, badChar],
        };
    }
}
