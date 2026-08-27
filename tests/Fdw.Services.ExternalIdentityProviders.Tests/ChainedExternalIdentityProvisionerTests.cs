using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Chained;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

using Fdw.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Tests;

/// <summary>
/// Behavior of the Chained composite <see cref="IExternalIdentityProvisioner"/>: step ordering, the
/// NotFound-fall-through contract, hard-error propagation, and the nested-Chained rejection rule.
/// </summary>
public sealed class ChainedExternalIdentityProvisionerTests
{
    private const string Provider = "test-idp";
    private const string ExternalSubject = "ext-subject-1";

    // Why: a standalone ResultCode reusing the canonical catalog NotFound number (30000) — the exact
    // contract IExternalIdentityProvisioner's NOT-FOUND CONTRACT documents. Any package's own NotFound
    // code (UserNotFoundCode, WorkflowNotFoundCode, ...) works identically here since the chain compares
    // on the numeric Id, never the prefixed Code string.
    private sealed class TestNotFoundResultCode : ResultCodeBase
    {
        public TestNotFoundResultCode()
            : base(30000, "TestNotFound", ResultSeverities.ByName("Error"), "Not found (test).", "TEST")
        {
        }
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ChainedExternalIdentityProvisionerConfiguration BuildTyped(params (string Name, int Order)[] steps)
    {
        var typed = new ChainedExternalIdentityProvisionerConfiguration { ExternalIdentityProvisionerId = Guid.NewGuid() };
        foreach (var (name, order) in steps)
            typed.Steps.Add(new ChainedProvisionerStepConfiguration { ProvisionerName = name, ExecutionOrder = order });
        return typed;
    }

    private static Mock<IExternalIdentityProvisioner> BuildLeafMock(string serviceType = "Leaf")
    {
        var mock = new Mock<IExternalIdentityProvisioner>(MockBehavior.Strict);
        mock.SetupGet(p => p.ServiceType).Returns(serviceType);
        return mock;
    }

    private static ChainedExternalIdentityProvisioner BuildSut(
        ChainedExternalIdentityProvisionerConfiguration typed,
        Mock<IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration>> providerMock)
    {
        var header = new ExternalIdentityProvisionerConfiguration { Name = "chained", Configuration = typed };
        return new ChainedExternalIdentityProvisioner(
            header, typed, providerMock.Object, NullLogger<ChainedExternalIdentityProvisioner>.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task MatchShortCircuitsBeforeLaterSteps()
    {
        var typed = BuildTyped(("First", 1), ("Second", 2));
        var userId = Guid.NewGuid();

        var firstMock = BuildLeafMock();
        firstMock
            .Setup(p => p.Provision(Provider, ExternalSubject, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Success(userId));

        var providerMock = new Mock<IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration>>(MockBehavior.Strict);
        providerMock.Setup(p => p.Get("First", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExternalIdentityProvisioner>.Success(firstMock.Object));
        // Why: Strict with no "Second" setup — a call to Get("Second") throws, proving the match short-circuited.

        var sut = BuildSut(typed, providerMock);

        var result = await sut.Provision(Provider, ExternalSubject, new ClaimsPrincipal(), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(userId);
        providerMock.Verify(p => p.Get("Second", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HardFailurePropagatesImmediatelyWithoutTryingLaterSteps()
    {
        var typed = BuildTyped(("First", 1), ("Second", 2));

        var firstMock = BuildLeafMock();
        firstMock
            .Setup(p => p.Provision(Provider, ExternalSubject, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Failure(new Fdw.Messages.GenericMessage("boom — a hard error, not a NotFound.")));

        var providerMock = new Mock<IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration>>(MockBehavior.Strict);
        providerMock.Setup(p => p.Get("First", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExternalIdentityProvisioner>.Success(firstMock.Object));

        var sut = BuildSut(typed, providerMock);

        var result = await sut.Provision(Provider, ExternalSubject, new ClaimsPrincipal(), Ct);

        result.IsSuccess.ShouldBeFalse();
        providerMock.Verify(p => p.Get("Second", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task NotFoundFallsThroughToNextStep()
    {
        var typed = BuildTyped(("First", 1), ("Second", 2));
        var userId = Guid.NewGuid();

        var firstMock = BuildLeafMock();
        firstMock
            .Setup(p => p.Provision(Provider, ExternalSubject, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Failure(new TestNotFoundResultCode()));

        var secondMock = BuildLeafMock();
        secondMock
            .Setup(p => p.Provision(Provider, ExternalSubject, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Success(userId));

        var providerMock = new Mock<IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration>>(MockBehavior.Strict);
        providerMock.Setup(p => p.Get("First", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExternalIdentityProvisioner>.Success(firstMock.Object));
        providerMock.Setup(p => p.Get("Second", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExternalIdentityProvisioner>.Success(secondMock.Object));

        var sut = BuildSut(typed, providerMock);

        var result = await sut.Provision(Provider, ExternalSubject, new ClaimsPrincipal(), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(userId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task FullFallThroughReturnsNotFound()
    {
        // Why: zero steps is the current shipped state (no leaf provisioner in this PR) — the chain
        // must resolve to nothing, byte-identical to today's default-OFF.
        var typed = BuildTyped();
        var providerMock = new Mock<IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration>>(MockBehavior.Strict);

        var sut = BuildSut(typed, providerMock);

        var result = await sut.Provision(Provider, ExternalSubject, new ClaimsPrincipal(), Ct);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task StepsRunInExecutionOrderEvenWhenSuppliedUnordered()
    {
        // Why: steps are added out of ExecutionOrder (2 then 1) — the chain must still walk 1 before 2.
        var typed = BuildTyped(("Second", 2), ("First", 1));

        var callOrder = new List<string>();

        var firstMock = BuildLeafMock();
        firstMock
            .Setup(p => p.Provision(Provider, ExternalSubject, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { callOrder.Add("First"); return GenericResult<Guid>.Failure(new TestNotFoundResultCode()); });

        var secondMock = BuildLeafMock();
        var userId = Guid.NewGuid();
        secondMock
            .Setup(p => p.Provision(Provider, ExternalSubject, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { callOrder.Add("Second"); return GenericResult<Guid>.Success(userId); });

        var providerMock = new Mock<IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration>>(MockBehavior.Strict);
        providerMock.Setup(p => p.Get("First", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExternalIdentityProvisioner>.Success(firstMock.Object));
        providerMock.Setup(p => p.Get("Second", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExternalIdentityProvisioner>.Success(secondMock.Object));

        var sut = BuildSut(typed, providerMock);

        var result = await sut.Provision(Provider, ExternalSubject, new ClaimsPrincipal(), Ct);

        result.IsSuccess.ShouldBeTrue();
        callOrder.ShouldBe(new[] { "First", "Second" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task StepResolutionFailurePropagatesWithoutTryingLaterSteps()
    {
        var typed = BuildTyped(("First", 1), ("Second", 2));

        var providerMock = new Mock<IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration>>(MockBehavior.Strict);
        providerMock.Setup(p => p.Get("First", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExternalIdentityProvisioner>.Failure(
                new Fdw.Messages.GenericMessage("provisioner 'First' is not registered.")));

        var sut = BuildSut(typed, providerMock);

        var result = await sut.Provision(Provider, ExternalSubject, new ClaimsPrincipal(), Ct);

        result.IsSuccess.ShouldBeFalse();
        providerMock.Verify(p => p.Get("Second", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task NestedChainedStepIsRejectedAndFallsThrough()
    {
        var typed = BuildTyped(("NestedChain", 1), ("Second", 2));
        var userId = Guid.NewGuid();

        // Why: Strict + ServiceType "Chained" + NO Provision setup — if the sut ever called Provision
        // on this instance (i.e. recursed), Moq would throw, failing the test.
        var nestedMock = BuildLeafMock(serviceType: "Chained");

        var secondMock = BuildLeafMock();
        secondMock
            .Setup(p => p.Provision(Provider, ExternalSubject, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Success(userId));

        var providerMock = new Mock<IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration>>(MockBehavior.Strict);
        providerMock.Setup(p => p.Get("NestedChain", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExternalIdentityProvisioner>.Success(nestedMock.Object));
        providerMock.Setup(p => p.Get("Second", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IExternalIdentityProvisioner>.Success(secondMock.Object));

        var sut = BuildSut(typed, providerMock);

        var result = await sut.Provision(Provider, ExternalSubject, new ClaimsPrincipal(), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(userId);
        nestedMock.Verify(
            p => p.Provision(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
