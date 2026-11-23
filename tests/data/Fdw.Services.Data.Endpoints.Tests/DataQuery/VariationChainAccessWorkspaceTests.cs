using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests.DataQuery;

/// <summary>
/// FDW-5446: on a host with workspaces, the owner of a Scoped variation is a caller who may change the workspace that owns it; a reader or an outsider holds the
/// variation without owning it; a decision that cannot be made is a failure; a data view stays with the user who owns it.
/// </summary>
public sealed class VariationChainAccessWorkspaceTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid Owner = Guid.NewGuid();

    private static VariationDataSetConfiguration Scoped() => new()
    {
        Id = Guid.NewGuid(), Name = "west", VariationName = "west", Implementation = VariationDataSetConfiguration.ImplementationName,
        OwnerWorkspaceId = WorkspaceId, BaseDataSetId = Guid.NewGuid(),
    };

    private static IWorkspaceConfigurationProvider Holding(IWorkspaceImplementationConfiguration? workspace, out Mock<IWorkspaceConfigurationProvider> provider)
    {
        provider = new Mock<IWorkspaceConfigurationProvider>();
        provider.Setup(p => p.Get(WorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IWorkspaceImplementationConfiguration>.Success(workspace!));
        return provider.Object;
    }

    private static IWorkspaceAccessPolicy MayWrite(IGenericResult answer)
    {
        var policy = new Mock<IWorkspaceAccessPolicy>();
        policy.Setup(p => p.MayWrite(It.IsAny<IWorkspaceImplementationConfiguration>(), It.IsAny<CancellationToken>())).ReturnsAsync(answer);
        return policy.Object;
    }

    private static VariationChainAccess Access(IWorkspaceConfigurationProvider workspaces, IWorkspaceAccessPolicy policy, string? userId = null)
    {
        var accessor = new AuthenticationContextAccessor { Current = null };
        if (userId is not null)
        {
            var caller = new Mock<IAuthenticationContext>();
            caller.Setup(c => c.UserId).Returns(userId);
            accessor.Current = caller.Object;
        }

        return new VariationChainAccess(workspaces, policy, accessor);
    }

    private static WorkspaceImplementationConfiguration Workspace() => new WorkspaceImplementationConfiguration { Id = WorkspaceId, Name = "West team" };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task ACallerWhoMayChangeTheOwningWorkspaceOwnsAScopedVariationsChain()
    {
        var answer = await Access(Holding(Workspace(), out _), MayWrite(GenericResult.Success())).MayShowChain(Scoped(), TestContext.Current.CancellationToken);

        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        answer.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task AReaderOrAnOutsiderHoldsTheVariationWithoutOwningIt()
    {
        var refused = GenericResult.Failure(WorkspacesResultCodes.ByName("WorkspaceNotVisible"), NullLogger.Instance);

        var answer = await Access(Holding(Workspace(), out _), MayWrite(refused)).MayShowChain(Scoped(), TestContext.Current.CancellationToken);

        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        answer.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task AnAccessDecisionThatCouldNotBeMadeIsAFailureAndNotANo()
    {
        var undecided = GenericResult.Failure(WorkspacesResultCodes.ByName("WorkspaceAccessUndetermined"), NullLogger.Instance);

        var answer = await Access(Holding(Workspace(), out _), MayWrite(undecided)).MayShowChain(Scoped(), TestContext.Current.CancellationToken);

        answer.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task AWorkspaceThatCannotBeFoundHasNoOneActingInItAndTheAccessPolicyIsNotAsked()
    {
        var policy = new Mock<IWorkspaceAccessPolicy>(MockBehavior.Strict);

        var answer = await Access(Holding(null, out _), policy.Object).MayShowChain(Scoped(), TestContext.Current.CancellationToken);

        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        answer.Value.ShouldBeFalse();
        policy.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task ADataViewStaysWithTheUserWhoOwnsItAndNoWorkspaceIsAsked()
    {
        var view = Scoped();
        view.OwnerWorkspaceId = null;
        view.OwnerUserId = Owner;
        var workspaces = new Mock<IWorkspaceConfigurationProvider>(MockBehavior.Strict);
        var policy = new Mock<IWorkspaceAccessPolicy>(MockBehavior.Strict);

        var mine = await Access(workspaces.Object, policy.Object, Owner.ToString()).MayShowChain(view, TestContext.Current.CancellationToken);
        var other = await Access(workspaces.Object, policy.Object, Guid.NewGuid().ToString()).MayShowChain(view, TestContext.Current.CancellationToken);

        mine.Value.ShouldBeTrue();
        other.Value.ShouldBeFalse();
        workspaces.VerifyNoOtherCalls();
        policy.VerifyNoOtherCalls();
    }
}
