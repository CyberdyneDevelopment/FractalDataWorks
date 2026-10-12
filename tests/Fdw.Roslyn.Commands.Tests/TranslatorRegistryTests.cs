using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Tests.TestDoubles;
using Fdw.Results;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Unit tests for <see cref="TranslatorRegistry"/>.
/// </summary>
public sealed class TranslatorRegistryTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetTranslatorGenericReturnsFailureWhenNoneRegistered()
    {
        // Arrange
        var registry = new TranslatorRegistry(NullLoggerFactory.Instance);

        // Act
        var result = registry.GetTranslator<IRoslynCommand, IRoslynCommandResult>();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("TranslatorNotFound");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void RegisterGenericThenGetTranslatorGenericReturnsSameInstance()
    {
        // Arrange
        var registry = new TranslatorRegistry(NullLoggerFactory.Instance);
        var translatorMock = new Mock<IRoslynCommandTranslator<FakeRoslynCommand, FakeCommandResult>>();

        // Act
        registry.Register(translatorMock.Object);
        var result = registry.GetTranslator<FakeRoslynCommand, FakeCommandResult>();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(translatorMock.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetTranslatorGenericReturnsFailureWhenResultTypeMismatches()
    {
        // Arrange
        var registry = new TranslatorRegistry(NullLoggerFactory.Instance);
        var translatorMock = new Mock<IRoslynCommandTranslator<FakeRoslynCommand, FakeCommandResult>>();
        registry.Register(translatorMock.Object);

        // Act — registered for FakeCommandResult, ask for a different result type
        var result = registry.GetTranslator<FakeRoslynCommand, FakeSnapshotCommandResult>();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("TranslatorNotFound");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetTranslatorByTypeReturnsFailureWhenCommandTypeIsNull()
    {
        // Arrange
        var registry = new TranslatorRegistry(NullLoggerFactory.Instance);

        // Act
        var result = registry.GetTranslator((Type)null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("CommandTypeCannotBeNull");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetTranslatorByTypeReturnsFailureWhenNoneRegistered()
    {
        // Arrange
        var registry = new TranslatorRegistry(NullLoggerFactory.Instance);

        // Act
        var result = registry.GetTranslator(typeof(FakeRoslynCommand));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("TranslatorNotFound");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void RegisterNonGenericThenGetTranslatorByTypeReturnsSameInstance()
    {
        // Arrange
        var registry = new TranslatorRegistry(NullLoggerFactory.Instance);
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock.SetupGet(t => t.CommandType).Returns(typeof(FakeRoslynCommand));

        // Act
        registry.Register(translatorMock.Object);
        var result = registry.GetTranslator(typeof(FakeRoslynCommand));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(translatorMock.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterNonGenericThrowsArgumentNullExceptionForNullTranslator()
    {
        // Arrange
        var registry = new TranslatorRegistry(NullLoggerFactory.Instance);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => registry.Register((IRoslynCommandTranslator)null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterGenericThrowsArgumentNullExceptionForNullTranslator()
    {
        // Arrange
        var registry = new TranslatorRegistry(NullLoggerFactory.Instance);

        // Act & Assert
        Should.Throw<ArgumentNullException>(
            () => registry.Register((IRoslynCommandTranslator<FakeRoslynCommand, FakeCommandResult>)null!));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void CountReflectsNumberOfDistinctRegisteredCommandTypes()
    {
        // Arrange
        var registry = new TranslatorRegistry(NullLoggerFactory.Instance);
        var translatorA = new Mock<IRoslynCommandTranslator>();
        translatorA.SetupGet(t => t.CommandType).Returns(typeof(FakeRoslynCommand));
        var translatorB = new Mock<IRoslynCommandTranslator>();
        translatorB.SetupGet(t => t.CommandType).Returns(typeof(FakeBaselineAwareCommand));

        // Act
        registry.Register(translatorA.Object);
        registry.Register(translatorB.Object);

        // Assert
        registry.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void RegisterTwiceForSameCommandTypeReplacesTranslatorAndKeepsCountStable()
    {
        // Arrange
        var registry = new TranslatorRegistry(NullLoggerFactory.Instance);
        var translatorA = new Mock<IRoslynCommandTranslator>();
        translatorA.SetupGet(t => t.CommandType).Returns(typeof(FakeRoslynCommand));
        var translatorB = new Mock<IRoslynCommandTranslator>();
        translatorB.SetupGet(t => t.CommandType).Returns(typeof(FakeRoslynCommand));

        // Act
        registry.Register(translatorA.Object);
        registry.Register(translatorB.Object);

        // Assert
        registry.Count.ShouldBe(1);
        registry.GetTranslator(typeof(FakeRoslynCommand)).Value.ShouldBeSameAs(translatorB.Object);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void HasTranslatorGenericReturnsTrueOnlyAfterRegistration()
    {
        // Arrange
        var registry = new TranslatorRegistry(NullLoggerFactory.Instance);

        // Act & Assert (before)
        registry.HasTranslator<FakeRoslynCommand>().ShouldBeFalse();

        // Act
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock.SetupGet(t => t.CommandType).Returns(typeof(FakeRoslynCommand));
        registry.Register(translatorMock.Object);

        // Assert (after)
        registry.HasTranslator<FakeRoslynCommand>().ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void HasTranslatorByTypeReturnsFalseForNullType()
    {
        // Arrange
        var registry = new TranslatorRegistry(NullLoggerFactory.Instance);

        // Act & Assert
        registry.HasTranslator((Type)null!).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void HasTranslatorByTypeReturnsTrueOnlyAfterRegistration()
    {
        // Arrange
        var registry = new TranslatorRegistry(NullLoggerFactory.Instance);
        registry.HasTranslator(typeof(FakeRoslynCommand)).ShouldBeFalse();
        var translatorMock = new Mock<IRoslynCommandTranslator>();
        translatorMock.SetupGet(t => t.CommandType).Returns(typeof(FakeRoslynCommand));

        // Act
        registry.Register(translatorMock.Object);

        // Assert
        registry.HasTranslator(typeof(FakeRoslynCommand)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task RegisteringATranslatorMakesItsLogLinesReachASink()
    {
        // The defect this closes: decoration used to be applied to the static TypeOption catalogue by
        // one host's DI wiring. Any translator that did not come from that catalogue — constructed
        // directly, registered through AddTranslator<T>, or built in a test — kept a NullLogger and ran
        // silently forever with no diagnostic. Registration is the choke point every executable
        // translator passes through, so it is the only place that closes the hole for all of them.
        var capture = new CapturingLoggerProvider();
        var probe = new LoggerProbeTranslator();

        probe.AttachedLogger.ShouldBeOfType<NullLogger>();

        new TranslatorRegistry(LoggerFactory.Create(b => b.AddProvider(capture))).Register(probe);

        // Run it the way the handler does, so what is asserted is delivery from inside a live command.
        await probe.Execute(
            new FakeRoslynCommand(), new AdhocWorkspace().CurrentSolution, TestContext.Current.CancellationToken);

        capture.Messages.ShouldContain(m => m.Contains(LoggerProbeTranslator.Marker, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AFailureKeepsItsResultCodeOnTheWayOutOfExecute()
    {
        // Execute used to rebuild every translator failure as a message-only result, discarding the code
        // the translator had chosen. Every failure in the system crossed that one line, so callers could
        // never branch on a code and the MCP layer had nothing but prose to report — a live run showed
        // "failed [(null)]" for a refusal that carried a perfectly good code one frame below.
        // The suite missed it because tests call Translate, which never crosses this boundary.
        var result = await new CodedFailureTranslator().Execute(
            new FakeRoslynCommand(), new AdhocWorkspace().CurrentSolution, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe(CodedFailureTranslator.FailureCode);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheRegistryRefusesToBeBuiltWithoutALoggerFactory()
    {
        // No NullLoggerFactory default on purpose: a silent fallback here would reintroduce exactly the
        // defect above, and it would look like working configuration.
        Should.Throw<ArgumentNullException>(() => new TranslatorRegistry(null!));
    }
}
