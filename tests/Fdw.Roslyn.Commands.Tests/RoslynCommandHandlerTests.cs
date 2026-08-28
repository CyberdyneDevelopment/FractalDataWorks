using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Tests.TestDoubles;
using Fdw.Workspace.Roslyn;
using Microsoft.CodeAnalysis;
using Fdw.Roslyn.Commands.Workspace.Results;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Unit tests for <see cref="RoslynCommandHandler"/> — the dispatch mechanism between translators and
/// the workspace, including the reflection-based baseline/snapshot injection special cases.
/// </summary>
public sealed class RoslynCommandHandlerTests
{
    private static Solution NewSolution() => new AdhocWorkspace().CurrentSolution;

    private static Mock<IRoslynWorkspace> NewWorkspaceMock(Solution? current = null)
    {
        var mock = new Mock<IRoslynWorkspace>();
        mock.SetupGet(w => w.CurrentSolution).Returns(current ?? NewSolution());
        // The handler reads the pending set before committing so it can tell "nothing to write" from
        // "the pending work disappeared". Default to empty; the tests that care set their own.
        mock.Setup(w => w.GetChangesFromBaseline())
            .Returns(new Dictionary<string, string>(StringComparer.Ordinal));
        return mock;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Generic Execute<TCommand, TResult>
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteGenericReturnsFailureWhenCommandIsNull()
    {
        // Arrange
        var handler = new RoslynCommandHandler(NewWorkspaceMock().Object, new Mock<ITranslatorRegistry>().Object);

        // Act
        var result = await handler.Execute<IRoslynCommand, IRoslynCommandResult>(null!, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("CommandCannotBeNull");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteGenericReturnsFailureWhenTranslatorNotFound()
    {
        // Arrange
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock
            .Setup(r => r.GetTranslator<IRoslynCommand, IRoslynCommandResult>())
            .Returns(GenericResult<IRoslynCommandTranslator<IRoslynCommand, IRoslynCommandResult>>.Failure(
                new GenericMessage("No translator registered")));
        var handler = new RoslynCommandHandler(NewWorkspaceMock().Object, registryMock.Object);

        // Act
        var result = await handler.Execute<IRoslynCommand, IRoslynCommandResult>(
            new FakeRoslynCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("TranslatorNotFound");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteGenericPropagatesTranslatorFailure()
    {
        // Arrange
        var translatorMock = new Mock<IRoslynCommandTranslator<IRoslynCommand, IRoslynCommandResult>>();
        translatorMock
            .Setup(t => t.Translate(It.IsAny<IRoslynCommand>(), It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Failure(new GenericMessage("translate failed")));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock
            .Setup(r => r.GetTranslator<IRoslynCommand, IRoslynCommandResult>())
            .Returns(GenericResult<IRoslynCommandTranslator<IRoslynCommand, IRoslynCommandResult>>.Success(translatorMock.Object));
        var handler = new RoslynCommandHandler(NewWorkspaceMock().Object, registryMock.Object);

        // Act
        var result = await handler.Execute<IRoslynCommand, IRoslynCommandResult>(
            new FakeRoslynCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("translate failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteGenericDoesNotUpdateWorkspaceWhenResultIsNotMutation()
    {
        // Arrange
        var fakeResult = new FakeCommandResult { IsMutation = false };
        var translatorMock = new Mock<IRoslynCommandTranslator<IRoslynCommand, IRoslynCommandResult>>();
        translatorMock
            .Setup(t => t.Translate(It.IsAny<IRoslynCommand>(), It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(fakeResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock
            .Setup(r => r.GetTranslator<IRoslynCommand, IRoslynCommandResult>())
            .Returns(GenericResult<IRoslynCommandTranslator<IRoslynCommand, IRoslynCommandResult>>.Success(translatorMock.Object));
        var workspaceMock = NewWorkspaceMock();
        var handler = new RoslynCommandHandler(workspaceMock.Object, registryMock.Object);

        // Act
        var result = await handler.Execute<IRoslynCommand, IRoslynCommandResult>(
            new FakeRoslynCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        workspaceMock.Verify(w => w.UpdateSolution(It.IsAny<Solution>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteGenericUpdatesWorkspaceWhenResultIsMutation()
    {
        // Arrange
        var newSolution = NewSolution();
        var fakeResult = new FakeCommandResult { IsMutation = true, NewSolution = newSolution };
        var translatorMock = new Mock<IRoslynCommandTranslator<IRoslynCommand, IRoslynCommandResult>>();
        translatorMock
            .Setup(t => t.Translate(It.IsAny<IRoslynCommand>(), It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(fakeResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock
            .Setup(r => r.GetTranslator<IRoslynCommand, IRoslynCommandResult>())
            .Returns(GenericResult<IRoslynCommandTranslator<IRoslynCommand, IRoslynCommandResult>>.Success(translatorMock.Object));
        var workspaceMock = NewWorkspaceMock();
        var handler = new RoslynCommandHandler(workspaceMock.Object, registryMock.Object);

        // Act
        var result = await handler.Execute<IRoslynCommand, IRoslynCommandResult>(
            new FakeRoslynCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        workspaceMock.Verify(w => w.UpdateSolution(newSolution), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteGenericReturnsCommandExecutionCancelledCodeWhenTranslatorThrowsOperationCanceledException()
    {
        // Arrange
        var translatorMock = new Mock<IRoslynCommandTranslator<IRoslynCommand, IRoslynCommandResult>>();
        translatorMock
            .Setup(t => t.Translate(It.IsAny<IRoslynCommand>(), It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock
            .Setup(r => r.GetTranslator<IRoslynCommand, IRoslynCommandResult>())
            .Returns(GenericResult<IRoslynCommandTranslator<IRoslynCommand, IRoslynCommandResult>>.Success(translatorMock.Object));
        var handler = new RoslynCommandHandler(NewWorkspaceMock().Object, registryMock.Object);

        // Act
        var result = await handler.Execute<IRoslynCommand, IRoslynCommandResult>(
            new FakeRoslynCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("CommandExecutionCancelled");

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteGenericReturnsExecutionFailedFailureWhenTranslatorThrows()
    {
        // Arrange
        var translatorMock = new Mock<IRoslynCommandTranslator<IRoslynCommand, IRoslynCommandResult>>();
        translatorMock
            .Setup(t => t.Translate(It.IsAny<IRoslynCommand>(), It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock
            .Setup(r => r.GetTranslator<IRoslynCommand, IRoslynCommandResult>())
            .Returns(GenericResult<IRoslynCommandTranslator<IRoslynCommand, IRoslynCommandResult>>.Success(translatorMock.Object));
        var handler = new RoslynCommandHandler(NewWorkspaceMock().Object, registryMock.Object);

        // Act
        var result = await handler.Execute<IRoslynCommand, IRoslynCommandResult>(
            new FakeRoslynCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("CommandExecutionFailed");
        result.Details.ShouldNotBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Non-generic Execute(IRoslynCommand)
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteReturnsFailureWhenCommandIsNull()
    {
        // Arrange
        var handler = new RoslynCommandHandler(NewWorkspaceMock().Object, new Mock<ITranslatorRegistry>().Object);

        // Act
        var result = await handler.Execute((IRoslynCommand)null!, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("CommandCannotBeNull");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteReturnsFailureWhenTranslatorNotFoundByType()
    {
        // Arrange
        var command = new FakeRoslynCommand();
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock
            .Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Failure(new GenericMessage("no translator")));
        var handler = new RoslynCommandHandler(NewWorkspaceMock().Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("TranslatorNotFound");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteDoesNotUpdateWorkspaceWhenResultIsNotMutation()
    {
        // Arrange
        var command = new FakeRoslynCommand();
        var fakeResult = new FakeCommandResult { IsMutation = false };
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(fakeResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var workspaceMock = NewWorkspaceMock();
        var handler = new RoslynCommandHandler(workspaceMock.Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        workspaceMock.Verify(w => w.UpdateSolution(It.IsAny<Solution>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteUpdatesWorkspaceWhenResultIsMutation()
    {
        // Arrange
        var command = new FakeRoslynCommand();
        var newSolution = NewSolution();
        var fakeResult = new FakeCommandResult { IsMutation = true, NewSolution = newSolution };
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(fakeResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var workspaceMock = NewWorkspaceMock();
        var handler = new RoslynCommandHandler(workspaceMock.Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        workspaceMock.Verify(w => w.UpdateSolution(newSolution), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteCallsSetBaselineAfterSuccessfulSetBaselineCommand()
    {
        // Arrange
        var command = new FakeSetBaselineCommand();
        var currentSolution = NewSolution();
        var fakeResult = new FakeCommandResult { IsMutation = false };
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(fakeResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var workspaceMock = NewWorkspaceMock(currentSolution);
        var handler = new RoslynCommandHandler(workspaceMock.Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        workspaceMock.Verify(w => w.SetBaseline(currentSolution), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteDoesNotCallSetBaselineWhenSetBaselineCommandFails()
    {
        // Arrange
        var command = new FakeSetBaselineCommand();
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Failure(new GenericMessage("failed")));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var workspaceMock = NewWorkspaceMock();
        var handler = new RoslynCommandHandler(workspaceMock.Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        workspaceMock.Verify(w => w.SetBaseline(It.IsAny<Solution>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteStoresRealSnapshotIdOnSuccessfulCreateSnapshotCommand()
    {
        // Arrange
        var command = new FakeCreateSnapshotCommand { SnapshotName = "my-snapshot", SnapshotDescription = "desc" };
        var fakeResult = new FakeSnapshotCommandResult();
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(fakeResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var workspaceMock = NewWorkspaceMock();
        workspaceMock.Setup(w => w.CreateSnapshot("my-snapshot", "desc")).Returns("real-id-123");
        var handler = new RoslynCommandHandler(workspaceMock.Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert — the real id arrives on the RETURNED result. It used to be written into the
        // translator's own object through reflection, past an `init` accessor that exists precisely to
        // forbid that; the caller now gets a result built around the id instead of a mutated one.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeOfType<MutationResult<SnapshotData>>()
            .Data.SnapshotId.ShouldBe("real-id-123");
        workspaceMock.Verify(w => w.CreateSnapshot("my-snapshot", "desc"), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteFailsRatherThanSilentlySkippingWhenSnapshotNameIsMissing()
    {
        // Arrange — CreateSnapshotTranslator already fails an empty name with SnapshotNameRequired, so
        // the handler agreeing is consistency, not a new rule. Previously it skipped the store in
        // silence and returned success carrying the translator's PLACEHOLDER id — an id that resolves
        // to no snapshot, so the caller's later RestoreSnapshot fails on a rollback they believed they
        // had. Only a double that bypasses the translator could reach this, but "succeeded, stored
        // nothing" is the exact failure shape worth refusing.
        var command = new FakeCreateSnapshotCommand { SnapshotName = string.Empty };
        var fakeResult = new FakeSnapshotCommandResult();
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(fakeResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var workspaceMock = NewWorkspaceMock();
        var handler = new RoslynCommandHandler(workspaceMock.Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("SnapshotNameRequired");
        workspaceMock.Verify(w => w.CreateSnapshot(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteAppliesWorkspaceChangesAndReturnsTheRealWrittenFileListOnSuccess()
    {
        // Arrange — the translator's own result is a placeholder; the handler must replace it
        // with the real ApplyChanges result rather than return the placeholder's empty list.
        var command = new FakeApplyWorkspaceChangesCommand();
        var placeholder = new QueryResult<IReadOnlyList<string>>("Pending", Array.Empty<string>());
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(placeholder));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var written = new List<string> { "/repo/Foo.cs", "/repo/Bar.cs" };
        var workspaceMock = NewWorkspaceMock();
        workspaceMock.Setup(w => w.ApplyChanges(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<string>>.Success(written));
        var handler = new RoslynCommandHandler(workspaceMock.Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var queryResult = result.Value.ShouldBeOfType<QueryResult<IReadOnlyList<string>>>();
        queryResult.Data.ShouldBe(written);
        workspaceMock.Verify(w => w.ApplyChanges(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteReturnsFailureWhenApplyWorkspaceChangesFails()
    {
        // Arrange
        var command = new FakeApplyWorkspaceChangesCommand();
        var placeholder = new QueryResult<IReadOnlyList<string>>("Pending", Array.Empty<string>());
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(placeholder));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var workspaceMock = NewWorkspaceMock();
        workspaceMock.Setup(w => w.ApplyChanges(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<string>>.Failure(new GenericMessage("disk write failed")));
        var handler = new RoslynCommandHandler(workspaceMock.Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("disk write failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteInjectsBaselineSolutionFromWorkspaceIntoBaselineAwareCommand()
    {
        // Arrange
        var command = new FakeBaselineAwareCommand();
        var baselineSolution = NewSolution();
        var fakeResult = new FakeCommandResult { IsMutation = false };
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(fakeResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var workspaceMock = NewWorkspaceMock();
        workspaceMock.SetupGet(w => w.BaselineSolution).Returns(baselineSolution);
        var handler = new RoslynCommandHandler(workspaceMock.Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        command.BaselineSolution.ShouldBeSameAs(baselineSolution);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteInjectsSnapshotSolutionWhenRestoreSnapshotSucceeds()
    {
        // Arrange
        var command = new FakeSnapshotAwareCommand { SnapshotId = "abc" };
        var restoredSolution = NewSolution();
        var fakeResult = new FakeCommandResult { IsMutation = false };
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(fakeResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var workspaceMock = NewWorkspaceMock();
        workspaceMock.Setup(w => w.RestoreSnapshot("abc")).Returns(GenericResult<Solution>.Success(restoredSolution));
        var handler = new RoslynCommandHandler(workspaceMock.Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        command.SnapshotSolution.ShouldBeSameAs(restoredSolution);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteDoesNotInjectSnapshotSolutionWhenRestoreSnapshotFails()
    {
        // Arrange
        var command = new FakeSnapshotAwareCommand { SnapshotId = "missing" };
        var fakeResult = new FakeCommandResult { IsMutation = false };
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(fakeResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var workspaceMock = NewWorkspaceMock();
        workspaceMock.Setup(w => w.RestoreSnapshot("missing"))
            .Returns(GenericResult<Solution>.Failure(new GenericMessage("not found")));
        var handler = new RoslynCommandHandler(workspaceMock.Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        command.SnapshotSolution.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteDoesNotAttemptSnapshotRestoreWhenSnapshotIdIsBlank()
    {
        // Arrange
        var command = new FakeSnapshotAwareCommand { SnapshotId = null };
        var fakeResult = new FakeCommandResult { IsMutation = false };
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(fakeResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var workspaceMock = NewWorkspaceMock();
        var handler = new RoslynCommandHandler(workspaceMock.Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        command.SnapshotSolution.ShouldBeNull();
        workspaceMock.Verify(w => w.RestoreSnapshot(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteReturnsCommandExecutionCancelledCodeWhenTranslatorThrowsOperationCanceledException()
    {
        // Arrange
        var command = new FakeRoslynCommand();
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var handler = new RoslynCommandHandler(NewWorkspaceMock().Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("CommandExecutionCancelled");

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteReturnsExecutionFailedFailureWhenTranslatorThrows()
    {
        // Arrange
        var command = new FakeRoslynCommand();
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var handler = new RoslynCommandHandler(NewWorkspaceMock().Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("CommandExecutionFailed");
        result.Details.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorThrowsArgumentNullExceptionForNullWorkspace()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new RoslynCommandHandler(null!, new Mock<ITranslatorRegistry>().Object));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorThrowsArgumentNullExceptionForNullTranslatorRegistry()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new RoslynCommandHandler(NewWorkspaceMock().Object, null!));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Change ledger recording
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteRecordsExactlyOneEntryIntoInjectedLedgerOnMutation()
    {
        // Arrange
        var command = new FakeRoslynCommand();
        var mutationResult = new MutationResult("did a thing", NewSolution());
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(mutationResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var ledger = new ChangeLedger();
        var handler = new RoslynCommandHandler(NewWorkspaceMock().Object, registryMock.Object, ledger);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        ledger.Entries.Count.ShouldBe(1);
        ledger.Entries[0].CommandName.ShouldBe(command.Name);
        ledger.Entries[0].Summary.ShouldBe("did a thing");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteDoesNotRecordLedgerEntryForNonMutationResult()
    {
        // Arrange
        var command = new FakeRoslynCommand();
        var queryResult = new QueryResult<object>("query summary", new object());
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(queryResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var ledger = new ChangeLedger();
        var handler = new RoslynCommandHandler(NewWorkspaceMock().Object, registryMock.Object, ledger);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        ledger.Entries.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteKeepsTheLedgerAcrossSetBaseline()
    {
        // Arrange
        var command = new FakeSetBaselineCommand();
        var currentSolution = NewSolution();
        var fakeResult = new FakeCommandResult { IsMutation = false };
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(fakeResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var ledger = new ChangeLedger();
        ledger.Record(
            "Rename", "prior mutation", Array.Empty<FileChange>(), Array.Empty<SymbolChange>(), Array.Empty<PathChange>());
        var handler = new RoslynCommandHandler(NewWorkspaceMock(currentSolution).Object, registryMock.Object, ledger);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // A baseline is "compare against here from now on"; the ledger is "what has been done". Wiping
        // the history because the comparison point moved destroyed the record the migration guide is
        // built from — and there was no way to ask for that, nor to avoid it. ClearChangeLedger is now
        // the only thing that discards it.
        ledger.Entries.ShouldHaveSingleItem().Summary.ShouldBe("prior mutation");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteRecordsMutationIntoNullChangeLedgerWithoutThrowingWhenNoLedgerIsSupplied()
    {
        // Arrange — 2-arg ctor: no ledger supplied, must default to NullChangeLedger without throwing.
        var command = new FakeRoslynCommand();
        var mutationResult = new MutationResult("did a thing", NewSolution());
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock
            .Setup(t => t.Execute(command, It.IsAny<Solution>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoslynCommandResult>.Success(mutationResult));
        var registryMock = new Mock<ITranslatorRegistry>();
        registryMock.Setup(r => r.GetTranslator(command.GetType()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translatorMock.Object));
        var handler = new RoslynCommandHandler(NewWorkspaceMock().Object, registryMock.Object);

        // Act
        var result = await handler.Execute(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
}
