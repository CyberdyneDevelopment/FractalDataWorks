using System.Security.Claims;
using Fdw.Claims.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.Services.Scheduling.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Scheduling;

/// <summary>
/// Saving a schedule whose runs act as the person (FDW-6144): the caller needs <c>schedules:run-as-me</c>, states when the grant ends, allows only what they hold themselves and allows at least everything the
/// schedule's run roles carry. Only then is a grant issued.
/// </summary>
public sealed class ScheduleRunAsGrantsTests
{
    private static readonly DateTimeOffset Ends = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly Mock<IRunGrants> _grants = new();
    private readonly Mock<IRoleGrants> _roles = new();
    private IReadOnlyCollection<string>? _issuedFor;

    private ScheduleRunAsGrants Sut()
    {
        _roles.Setup(r => r.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(["pipelines:execute"]));
        _grants
            .Setup(g => g.Issue(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<string>, DateTimeOffset, CancellationToken>((permissions, _, _) => _issuedFor = permissions)
            .ReturnsAsync(GenericResult<RunGrant>.Success(new RunGrant { Id = Guid.NewGuid(), ExpiresAt = Ends, Permissions = ["pipelines:execute"] }));
        return new ScheduleRunAsGrants(_grants.Object, _roles.Object);
    }

    private static ClaimsPrincipalAuthenticationContext Caller(params string[] permissions) => new(new ClaimsPrincipal(
        new ClaimsIdentity([.. permissions.Select(permission => new Claim(ClaimDefinitions.perm.Name, permission)), new Claim(ClaimDefinitions.sub.Name, Guid.NewGuid().ToString())], "Test")));

    private static ScheduleRunAsRequest Request(DateTimeOffset? ends, params string[] permissions) => new() { Permissions = [.. permissions], ExpiresAt = ends };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerWhoHoldsThePermissionAndWhatTheyAllowGetsAGrantForExactlyWhatTheyStated()
    {
        var issued = await Sut().Issue(
            Caller("schedules:run-as-me", "pipelines:execute"), "Nightly", Request(Ends, "pipelines:execute"), ["etl-runner"], NullLogger.Instance, TestContext.Current.CancellationToken);

        issued.IsSuccess.ShouldBeTrue(issued.CurrentMessage);
        _issuedFor.ShouldNotBeNull().ShouldBe(["pipelines:execute"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerWithoutThePermissionIsRefusedAndNoGrantIsIssued()
    {
        var issued = await Sut().Issue(
            Caller("pipelines:execute"), "Nightly", Request(Ends, "pipelines:execute"), ["etl-runner"], NullLogger.Instance, TestContext.Current.CancellationToken);

        issued.IsFailure.ShouldBeTrue();
        issued.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("ENDPOINTS10-101003");
        _issuedFor.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AGrantWithNoStatedEndIsRefused()
    {
        var issued = await Sut().Issue(
            Caller("schedules:run-as-me", "pipelines:execute"), "Nightly", Request(null, "pipelines:execute"), ["etl-runner"], NullLogger.Instance, TestContext.Current.CancellationToken);

        issued.IsFailure.ShouldBeTrue();
        issued.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("ENDPOINTS10-21004");
        _issuedFor.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AGrantNeverAllowsMoreThanThePersonWhoGivesItHolds()
    {
        var issued = await Sut().Issue(
            Caller("schedules:run-as-me", "pipelines:execute"), "Nightly", Request(Ends, "pipelines:execute", "connections:write"), ["etl-runner"], NullLogger.Instance, TestContext.Current.CancellationToken);

        issued.IsFailure.ShouldBeTrue();
        issued.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("ENDPOINTS10-101005");
        issued.CurrentMessage.ShouldNotBeNull().ShouldContain("connections:write");
        _issuedFor.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AGrantThatAllowsLessThanTheRunRolesCarryIsRefusedSoNoFireIsRefusedEveryTime()
    {
        var issued = await Sut().Issue(
            Caller("schedules:run-as-me", "pipelines:execute", "datasets:read"), "Nightly", Request(Ends, "datasets:read"), ["etl-runner"], NullLogger.Instance, TestContext.Current.CancellationToken);

        issued.IsFailure.ShouldBeTrue();
        issued.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("ENDPOINTS10-21006");
        issued.CurrentMessage.ShouldNotBeNull().ShouldContain("pipelines:execute");
        _issuedFor.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Behavior")]
    public async Task RevokingNoGrantDoesNothingAndRevokingOneTakesItBack()
    {
        var sut = Sut();
        var grant = Guid.NewGuid();
        _grants.Setup(g => g.Revoke(grant, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        await sut.Revoke(null, TestContext.Current.CancellationToken);
        _grants.Verify(g => g.Revoke(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        await sut.Revoke(grant, TestContext.Current.CancellationToken);
        _grants.Verify(g => g.Revoke(grant, It.IsAny<CancellationToken>()), Times.Once);
    }
}
