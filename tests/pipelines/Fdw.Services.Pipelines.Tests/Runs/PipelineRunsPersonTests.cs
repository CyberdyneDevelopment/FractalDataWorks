using Fdw.Services.Abstractions;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Pipelines.Runs;
using Fdw.Services.Pipelines.TestDouble;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Tests.Runs;

/// <summary>
/// Tests for the run's person (FDW-6137): <see cref="PipelineRuns"/> fills it from the caller, only from the caller; a start that names another
/// person is refused; a caller that is no person starts a run with no person, which runs exactly as before.
/// </summary>
public sealed class PipelineRunsPersonTests
{
    private static readonly Guid Person = Guid.Parse("66666666-0000-0000-0000-000000000006");
    private static readonly Guid Other = Guid.Parse("77777777-0000-0000-0000-000000000007");
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Execution = Guid.Parse("33333333-0000-0000-0000-000000000003");

    private readonly Mock<IPipelineConfigurationProvider> _pipelines = new();
    private readonly Mock<IPipelineRunner> _queued = new();
    private readonly Mock<IPipelineImplementationConfiguration> _pipeline = new();
    private readonly Mock<IAuthenticationContextAccessor> _accessor = new();
    private PipelineStart? _handed;

    public PipelineRunsPersonTests()
    {
        _pipeline.SetupGet(p => p.Id).Returns(Guid.NewGuid());
        _pipeline.SetupGet(p => p.Name).Returns("Consolidate");
        _pipeline.SetupGet(p => p.Implementation).Returns("Graph");
        _pipelines.Setup(p => p.Get(It.IsAny<string>(),ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(_pipeline.Object));
        _queued.SetupGet(r => r.RunMode).Returns(PipelineRunModes.ByName("Queued"));
        _queued
            .Setup(r => r.Start(It.IsAny<PipelineStart>(), It.IsAny<IPipelineImplementationConfiguration>(), It.IsAny<CancellationToken>()))
            .Callback<PipelineStart, IPipelineImplementationConfiguration, CancellationToken>((start, _, _) => _handed = start)
            .ReturnsAsync(GenericResult<Guid>.Success(Execution));
    }

    private PipelineRuns Sut() => new(
        _pipelines.Object, [_queued.Object], Mock.Of<IExecutionRunControl>(), Mock.Of<IExecutionTracker>(), _accessor.Object,
        NullLogger<PipelineRuns>.Instance);

    private static PipelineStart Start(Guid? person = null) => new()
    {
        PipelineName = "Consolidate",
        TriggerSource = "Api",
        TenantId = Tenant,
        RunRoles = ["etl-runner"],
        Parameters = [],
        AwaitCompletion = false,
        PersonId = person
    };

    private void CallerIs(IAuthenticationContext context) => _accessor.SetupGet(a => a.Current).Returns(context);

    private static ClaimsPrincipalAuthenticationContext PersonCaller(Guid person) => new ClaimsPrincipalAuthenticationContext(new ClaimsPrincipal(
        new ClaimsIdentity([new Claim(ClaimDefinitions.sub.Name, person.ToString()), new Claim(ClaimDefinitions.tenantId.Name, Tenant.ToString())], "Test")));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARunAPersonStartsActsAsThatPersonFilledFromTheCaller()
    {
        CallerIs(PersonCaller(Person));

        var result = await Sut().Start(Start(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _handed.ShouldNotBeNull().PersonId.ShouldBe(Person);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStartThatNamesAnotherPersonThanTheCallerIsRefusedAsRunPersonMismatch()
    {
        CallerIs(PersonCaller(Person));

        var result = await Sut().Start(Start(Other), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("PIPELINERUNS-100830");
        _handed.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStartThatNamesAPersonFromACallerThatIsNoPersonIsRefused()
    {
        CallerIs(new SystemAuthenticationContext(Tenant));

        var result = await Sut().Start(Start(Person), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("PIPELINERUNS-100830");
        _handed.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStartThatNamesTheCallerItselfIsTaken()
    {
        CallerIs(PersonCaller(Person));

        var result = await Sut().Start(Start(Person), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _handed.ShouldNotBeNull().PersonId.ShouldBe(Person);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARunTheSystemOrARunsOwnPrincipalStartsHasNoPersonAndRunsAsBefore()
    {
        CallerIs(new SystemAuthenticationContext(Tenant));
        await Sut().Start(Start(), TestContext.Current.CancellationToken);
        _handed.ShouldNotBeNull().PersonId.ShouldBeNull();

        _handed = null;
        CallerIs(new TenantRunAuthenticationContext(Guid.NewGuid(), "Nightly", Tenant, ["etl-runner"], []));
        await Sut().Start(Start(), TestContext.Current.CancellationToken);
        _handed.ShouldNotBeNull().PersonId.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AHeldRunApprovedByAnotherPersonActsAsThePersonWhoAskedForIt()
    {
        CallerIs(PersonCaller(Other));

        var result = await Sut().Start(
            Start(Person) with { Opened = new PipelineStartOpened(Execution, null) { ReleasesHeldRun = true } },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _handed.ShouldNotBeNull().PersonId.ShouldBe(Person);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AHeldRunAskedForByNoPersonActsAsNoPersonWhoeverApprovesIt()
    {
        CallerIs(PersonCaller(Other));

        var result = await Sut().Start(
            Start() with { Opened = new PipelineStartOpened(Execution, null) { ReleasesHeldRun = true } },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _handed.ShouldNotBeNull().PersonId.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOpenedStartThatReleasesNoHeldRunCannotNameAnotherPersonThanItsCaller()
    {
        CallerIs(PersonCaller(Other));

        var result = await Sut().Start(
            Start(Person) with { Opened = new PipelineStartOpened(Execution, null) },
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("PIPELINERUNS-100830");
        _handed.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AScheduledFireWhoseGrantNamesAPersonActsAsThemThoughItsCallerIsNoPerson()
    {
        CallerIs(new SystemAuthenticationContext(Tenant));

        var result = await Sut().Start(
            Start(Person) with { Opened = new PipelineStartOpened(Execution, null) { ActsAsGrantedPerson = true } },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _handed.ShouldNotBeNull().PersonId.ShouldBe(Person);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStartThatNamesAPersonWithoutTheServersMarkOfAGrantIsStillRefused()
    {
        CallerIs(new SystemAuthenticationContext(Tenant));

        var result = await Sut().Start(
            Start(Person) with { Opened = new PipelineStartOpened(Execution, null) },
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("PIPELINERUNS-100830");
        _handed.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStartOfATypeThatRunsForAPersonIsRefusedWhenTheCallerIsNoneAndHandsNothingToTheRunner()
    {
        _pipeline.SetupGet(p => p.Implementation).Returns(PersonOnlyPipelineType.OptionName);
        CallerIs(new SystemAuthenticationContext(Tenant));

        var result = await Sut().Start(Start(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("PIPELINERUNS-20832");
        _handed.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AStartOfATypeThatRunsForAPersonByAPersonIsHandedToTheRunnerWithThatPerson()
    {
        _pipeline.SetupGet(p => p.Implementation).Returns(PersonOnlyPipelineType.OptionName);
        CallerIs(PersonCaller(Person));

        var result = await Sut().Start(Start(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _handed.ShouldNotBeNull().PersonId.ShouldBe(Person);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ATypeThatDoesNotRunForAPersonStillStartsWithNoPerson()
    {
        CallerIs(new SystemAuthenticationContext(Tenant));

        var result = await Sut().Start(Start(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _handed.ShouldNotBeNull().PersonId.ShouldBeNull();
    }
}
