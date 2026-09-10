using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Aegis.Abstractions;
using Fdw.Aegis.Configuration;
using Fdw.Results;
using Fdw.Services.Connections.Http;
using Fdw.Services.Data.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
/// <para>
/// The declared commands are the AegisCommand domain, read through
/// <see cref="IAegisCommandConfigurationProvider"/>; the fixture registers one that answers from
/// memory before the host registers its own, which TryAdd then leaves in place.
/// </para>
/// <para>
/// Open (fdw-727-aegis): <see cref="ConfigurationSchema"/> no longer declares secret managers and this
/// host registers no other source of secret-manager configuration, so "EnvSecrets" has nowhere to
/// resolve from until that is wired; the cases that inject a secret depend on it.
/// </para>
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

        builder.Services.AddSingleton(CommandProvider());

        AegisHostRegistration.Configure(builder, loggerFactory: null);
        AegisHostRegistration.Register(builder, schema, loggerFactory: null);

        Host = builder.Build();

        AegisHostRegistration.Initialize(Host, loggerFactory: null);
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

    private static ConfigurationSchema BuildSchema(string stubAddress, string hostileAddress) => new()
    {
        Connections =
        [
            new HttpConnectionConfiguration { Name = "synthetic-echo", Implementation = "Http", BaseUrl = stubAddress },
            new HttpConnectionConfiguration { Name = "hostile-echo", Implementation = "Http", BaseUrl = hostileAddress },
        ],
    };

    private static IAegisCommandConfigurationProvider CommandProvider()
    {
        IReadOnlyList<IApprovalPolicyConfiguration> commands =
        [
            PreApproved("echo_credential", "synthetic-echo", "AEGIS_SYNTHETIC_TOKEN"),
            new AdHocCommandConfiguration
            {
                Name = "echo_adhoc",
                Domain = "AegisCommand",
                Implementation = "AdHoc",
                ConnectionName = "synthetic-echo",
                SecretManagerName = "EnvSecrets",
                SecretKeyName = "AEGIS_SYNTHETIC_TOKEN",
            },
            PreApproved("echo_hostile", "hostile-echo", "AEGIS_SYNTHETIC_TOKEN"),
            PreApproved("echo_badchar", "synthetic-echo", "AEGIS_BADCHAR_TOKEN"),
        ];

        var provider = new Mock<IAegisCommandConfigurationProvider>();
        provider.Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IApprovalPolicyConfiguration>>.Success(commands));
        return provider.Object;
    }

    private static PreApprovedCommandConfiguration PreApproved(string name, string connectionName, string secretKeyName) => new()
    {
        Name = name,
        Domain = "AegisCommand",
        Implementation = "PreApproved",
        ConnectionName = connectionName,
        SecretManagerName = "EnvSecrets",
        SecretKeyName = secretKeyName,
        ParameterAllowList =
        [
            new ParameterAllowEntryConfiguration
            {
                ParameterName = "mode",
                Required = true,
                PermittedValues = [new PermittedValueConfiguration { Value = "echo" }],
            },
        ],
    };
}
