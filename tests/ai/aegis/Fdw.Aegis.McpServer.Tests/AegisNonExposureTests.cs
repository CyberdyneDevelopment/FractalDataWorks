using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Aegis.McpServer.Tests;

/// <summary>
/// The non-exposure proof for the Aegis Gateway MCP host: composes the REAL DI graph (real
/// <c>EnvironmentVariableSecretManager</c> via three-phase, real <see cref="AegisInjector"/>, real
/// <see cref="Fdw.Aegis.Targets.HttpHeaderInjectionTarget"/>, real
/// <see cref="Fdw.Aegis.PreApprovedPolicyEvaluator"/>, real <see cref="AegisToolService"/>) against
/// synthetic downstream endpoints and asserts the resolved secret never crosses the boundary — not in
/// the tool's returned JSON, not in a log line — while proving the real inject path actually ran.
/// </summary>
/// <remarks>
/// Whether the credential ACTUALLY reached the downstream is proven SERVER-SIDE (the stub records a
/// fingerprint of the header it received); what crosses back to Claude is asserted clean independently.
/// The hostile-downstream and header-invalid-secret cases exercise the exact leak paths a polite stub
/// cannot: a downstream reflecting the credential in its body, and a secret whose value would
/// otherwise be embedded in a header-format exception message.
/// </remarks>
[Trait("Category", "Security")]
public sealed class AegisNonExposureTests : IClassFixture<AegisTestFixture>
{
    private readonly AegisTestFixture _fixture;

    public AegisNonExposureTests(AegisTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RequestActionSucceedsWithoutExposingTheToken()
    {
        using var scope = _fixture.Host.Services.CreateScope();
        var toolService = CreateToolService(scope);
        var ct = TestContext.Current.CancellationToken;

        var result = await toolService.RequestAction("synthetic-echo", "echo_credential", "{\"mode\":\"echo\"}", ct);

        result.ShouldContain("\"success\":true");
        result.ShouldContain("\"ref\"");
        result.ShouldContain("status=200");
        result.ShouldNotContain(_fixture.Token);
    }

    [Fact]
    public async Task RequestActionActuallyReachesTheDownstreamWithTheRealCredential()
    {
        using var scope = _fixture.Host.Services.CreateScope();
        var toolService = CreateToolService(scope);
        var ct = TestContext.Current.CancellationToken;
        var before = _fixture.Stub.RequestCount;

        var result = await toolService.RequestAction("synthetic-echo", "echo_credential", "{\"mode\":\"echo\"}", ct);

        // Proven SERVER-SIDE: the stub recorded a fingerprint of the exact Authorization header it
        // received, and it matches SHA256("Bearer <token>") — so the real credential traversed the
        // real injector into the real downstream call. This reads the stub's own state, NOT anything
        // the gateway returned to the caller.
        _fixture.Stub.RequestCount.ShouldBe(before + 1);
        _fixture.Stub.LastAuthorizationFingerprint.ShouldBe(SyntheticEchoStub.FingerprintOf($"Bearer {_fixture.Token}"));

        // The sanitized receipt the gateway DID return carries only a status + a body fingerprint.
        result.ShouldNotContain(_fixture.Token);
    }

    [Fact]
    public async Task RequestActionAgainstAHostileDownstreamThatEchoesTheTokenDoesNotLeakIt()
    {
        using var scope = _fixture.Host.Services.CreateScope();
        var toolService = CreateToolService(scope);
        var ct = TestContext.Current.CancellationToken;
        var before = _fixture.HostileStub.RequestCount;

        var result = await toolService.RequestAction("hostile-echo", "echo_hostile", "{\"mode\":\"echo\"}", ct);

        // The hostile downstream really received the credential (proving the call happened) and echoed
        // it back in its 200 body...
        _fixture.HostileStub.RequestCount.ShouldBe(before + 1);
        _fixture.HostileStub.LastAuthorizationFingerprint.ShouldBe(SyntheticEchoStub.FingerprintOf($"Bearer {_fixture.Token}"));

        // ...yet the gateway surfaces NONE of the reflected credential to the caller. This is the case
        // a polite stub could never test.
        result.ShouldContain("\"success\":true");
        result.ShouldNotContain(_fixture.Token);
    }

    [Fact]
    public async Task RequestActionWithAHeaderInvalidSecretFailsWithoutLeakingItAndNeverCallsDownstream()
    {
        using var scope = _fixture.Host.Services.CreateScope();
        var toolService = CreateToolService(scope);
        var ct = TestContext.Current.CancellationToken;
        var before = _fixture.Stub.RequestCount;

        var result = await toolService.RequestAction("synthetic-echo", "echo_badchar", "{\"mode\":\"echo\"}", ct);

        result.ShouldContain("\"success\":false");
        result.ShouldNotContain(_fixture.BadCharToken);
        // Positive proof that IsValidHeaderValue specifically fired (not an unrelated resolution
        // failure that would also leave the downstream untouched): the surfaced reason is the
        // header-invalidity message.
        result.ShouldContain("not a valid HTTP header value");
        // Rejected BEFORE any header was built or any request sent — the downstream was never touched.
        _fixture.Stub.RequestCount.ShouldBe(before);
    }

    [Fact]
    public async Task DescribeActionExposesTheAllowListButNeverTheSecretReference()
    {
        using var scope = _fixture.Host.Services.CreateScope();
        var toolService = CreateToolService(scope);
        var ct = TestContext.Current.CancellationToken;

        var result = await toolService.DescribeAction("echo_credential", ct);

        result.ShouldContain("\"mode\"");
        result.ShouldContain("\"echo\"");
        result.ShouldNotContain("AEGIS_SYNTHETIC_TOKEN");
        result.ShouldNotContain("EnvSecrets");
        result.ShouldNotContain(_fixture.Token);
    }

    [Fact]
    public async Task RequestActionDeniesAnAdHocCommandAndNeverCallsTheDownstream()
    {
        using var scope = _fixture.Host.Services.CreateScope();
        var toolService = CreateToolService(scope);
        var ct = TestContext.Current.CancellationToken;
        var before = _fixture.Stub.RequestCount;

        var result = await toolService.RequestAction("synthetic-echo", "echo_adhoc", "{}", ct);

        result.ShouldContain("\"success\":false");
        _fixture.Stub.RequestCount.ShouldBe(before);
    }

    [Fact]
    public async Task RequestActionRejectsAParameterOutsideTheAllowListAndNeverCallsTheDownstream()
    {
        using var scope = _fixture.Host.Services.CreateScope();
        var toolService = CreateToolService(scope);
        var ct = TestContext.Current.CancellationToken;
        var before = _fixture.Stub.RequestCount;

        var result = await toolService.RequestAction("synthetic-echo", "echo_credential", "{\"mode\":\"exfiltrate\"}", ct);

        result.ShouldContain("\"success\":false");
        _fixture.Stub.RequestCount.ShouldBe(before);
    }

    [Fact]
    public async Task NoLogLineEverContainsAnySecret()
    {
        using var scope = _fixture.Host.Services.CreateScope();
        var toolService = CreateToolService(scope);
        var ct = TestContext.Current.CancellationToken;

        await toolService.RequestAction("synthetic-echo", "echo_credential", "{\"mode\":\"echo\"}", ct);
        await toolService.DescribeAction("echo_credential", ct);
        await toolService.RequestAction("synthetic-echo", "echo_adhoc", "{}", ct);
        await toolService.RequestAction("hostile-echo", "echo_hostile", "{\"mode\":\"echo\"}", ct);
        await toolService.RequestAction("synthetic-echo", "echo_badchar", "{\"mode\":\"echo\"}", ct);

        foreach (var line in _fixture.LogCollector.Lines)
        {
            line.ShouldNotContain(_fixture.Token);
            line.ShouldNotContain(_fixture.BadCharToken);
        }
    }

    private static AegisToolService CreateToolService(IServiceScope scope) =>
        ActivatorUtilities.CreateInstance<AegisToolService>(scope.ServiceProvider);
}
