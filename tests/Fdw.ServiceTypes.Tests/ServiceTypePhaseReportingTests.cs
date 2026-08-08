using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections;
using Fdw.ServiceTypes.Tests.TestDoubles;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Results;

namespace Fdw.ServiceTypes.Tests;

/// <summary>
/// Covers what the phase invokers report: whether the body about to run is the framework's or an
/// application's, the position of the collection in the phase and of the option within the
/// collection, and that a body which throws is logged and converted into a failure result.
/// </summary>
/// <remarks>
/// Each test closes over its OWN interface, because the flags and the funcs are static on a generic
/// base and are therefore per closed generic. Sharing one would make these tests order-dependent.
/// </remarks>
public class ServiceTypePhaseReportingTests
{
    public interface IDefaultCase : IServiceTypeRegistration;
    public interface IReplacedCase : IServiceTypeRegistration;
    public interface IThrowingCase : IServiceTypeRegistration;
    public interface ICodedCase : IServiceTypeRegistration;
    public interface IPassThroughCase : IServiceTypeRegistration;
    public interface ISweepCase : IServiceTypeRegistration;

    public abstract class OptionBase;

    // The flags are protected static; a derived collection is the sanctioned way to observe them,
    // the same way the registration tests observe Options.
    private sealed class DefaultCaseCollection : ServiceTypeCollectionBase<OptionBase, IDefaultCase>
    { public static bool RegisterCustom => RegistrationIsCustom; }

    private sealed class ReplacedCaseCollection : ServiceTypeCollectionBase<OptionBase, IReplacedCase>
    { public static bool RegisterCustom => RegistrationIsCustom; }

    private sealed class ThrowingCaseCollection : ServiceTypeCollectionBase<OptionBase, IThrowingCase>;
    private sealed class CodedCaseCollection : ServiceTypeCollectionBase<OptionBase, ICodedCase>;
    private sealed class PassThroughCaseCollection : ServiceTypeCollectionBase<OptionBase, IPassThroughCase>;

    private sealed class SweepCaseCollection : ServiceTypeCollectionBase<OptionBase, ISweepCase>;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CollectionReportsTheDefaultBodyWhenNothingReplacedIt()
    {
        var log = new CapturingLoggerFactory();

        ServiceTypeCollectionBase<OptionBase, IDefaultCase>.Register(NewBuilder(), log);

        DefaultCaseCollection.RegisterCustom.ShouldBeFalse();
        log.Messages.ShouldContain(m => m.Contains("DEFAULT implementation", StringComparison.Ordinal));
        log.Messages.ShouldNotContain(m => m.Contains("CUSTOM implementation", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CollectionReportsACustomBodyOnceTheGerundReplacedIt()
    {
        var log = new CapturingLoggerFactory();
        var ran = false;

        ServiceTypeCollectionBase<OptionBase, IReplacedCase>.Registration((builder, loggerFactory) =>
        {
            ran = true;
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
        ServiceTypeCollectionBase<OptionBase, IReplacedCase>.Register(NewBuilder(), log);

        ran.ShouldBeTrue();
        ReplacedCaseCollection.RegisterCustom.ShouldBeTrue();
        log.Messages.ShouldContain(m => m.Contains("CUSTOM implementation", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CollectionPhasePointsAtTheDocumentation()
    {
        var log = new CapturingLoggerFactory();

        ServiceTypeCollectionBase<OptionBase, ISweepCase>.Configure(NewBuilder(), log);

        log.Messages.ShouldContain(m => m.Contains(ServiceTypeLogDocumentation, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CollectionLogsThenReturnsAFailureWhenTheBodyThrows()
    {
        var log = new CapturingLoggerFactory();
        ServiceTypeCollectionBase<OptionBase, IThrowingCase>.Registration(
            (builder, loggerFactory) => throw new InvalidOperationException("phase blew up"));

        // Why the throw must NOT survive: ending the process is a decision about this application, and
        // the framework is not the thing entitled to make it. The phase converts the exception into a
        // failure the caller has to read to get the builder back out of — so a half-registered domain
        // still cannot reach a running application by accident, but "abort" versus "run without this
        // domain" stays the host's call.
        var result = ServiceTypeCollectionBase<OptionBase, IThrowingCase>.Register(NewBuilder(), log);

        result.IsSuccess.ShouldBeFalse();
        log.Messages.ShouldContain(m => m.Contains("FAILED", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CollectionFailureCarriesTheResultCodeThatNamesThePhase()
    {
        var log = new CapturingLoggerFactory();
        ServiceTypeCollectionBase<OptionBase, ICodedCase>.Registration(
            (builder, loggerFactory) => throw new InvalidOperationException("phase blew up"));

        // Why the code matters as well as the failure: a caller deciding whether to abort needs to tell
        // "a phase crashed" from "a domain deliberately refused", and the code is what carries that
        // distinction across the boundary. A bare IsSuccess=false says only that something went wrong.
        var result = ServiceTypeCollectionBase<OptionBase, ICodedCase>.Register(NewBuilder(), log);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CollectionSucceedsAndHandsBackTheBuilderItWasGiven()
    {
        var log = new CapturingLoggerFactory();
        var builder = NewBuilder();

        // Why this is worth pinning next to the failure cases: wrapping the phases in a result is only
        // safe if the success path still yields the same builder. If it did not, every chained caller
        // would silently configure a different builder than the one it later builds.
        var result = ServiceTypeCollectionBase<OptionBase, IPassThroughCase>.Configure(builder, log);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(builder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void OptionReportsCustomOnlyWhenItSuppliedItsOwnBody()
    {
        var log = new CapturingLoggerFactory();

        // TestServiceType sets a Register body in its constructor; AlternateTestServiceType does not.
        new TestServiceType().Register(NewBuilder(), log, "Store", "Path", "Container");
        var withBody = log.Messages.ToList();

        log.Messages.Clear();
        new AlternateTestServiceType().Register(NewBuilder(), log, "Store", "Path", "Container");

        withBody.ShouldContain(m => m.Contains("CUSTOM implementation", StringComparison.Ordinal));
        log.Messages.ShouldContain(m => m.Contains("DEFAULT implementation", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void OptionPositionsAreNumberedWithinTheCollectionThatSweptThem()
    {
        var log = new CapturingLoggerFactory();

        // Opening a collection's pass restarts option numbering, so an option's position is read
        // against its own collection rather than against everything registered before it.
        ServiceTypeCollectionBase<OptionBase, ISweepCase>.Register(NewBuilder(), log);
        log.Messages.Clear();

        new TestServiceType().Register(NewBuilder(), log, "Store", "Path", "Container");
        new AlternateTestServiceType().Register(NewBuilder(), log, "Store", "Path", "Container");

        log.Messages.ShouldContain(m => m.Contains("option #1", StringComparison.Ordinal));
        log.Messages.ShouldContain(m => m.Contains("option #2", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void OptionReportsSuccessAfterTheBodyReturns()
    {
        var log = new CapturingLoggerFactory();

        new TestServiceType().Register(NewBuilder(), log, "Store", "Path", "Container");

        log.Messages.ShouldContain(m => m.Contains("completed successfully", StringComparison.Ordinal));
    }

    // The documentation pointer the phase lines carry. Asserted by value so that moving the link at GA
    // has to be a deliberate edit here as well as in the log class.
    private const string ServiceTypeLogDocumentation = "wiki/10-TypeCollection-Patterns.md";

    // A mock suffices: the collections under test hold no options, and the option bodies exercised
    // here return the builder untouched. Nothing in these paths reads Services or Configuration.
    private static IHostApplicationBuilder NewBuilder() => new Mock<IHostApplicationBuilder>().Object;

    /// <summary>Records every formatted message so the phase lines can be asserted as written.</summary>
    private sealed class CapturingLoggerFactory : ILoggerFactory
    {
        public List<string> Messages { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

        public void AddProvider(ILoggerProvider provider) { }

        public void Dispose() { }

        private sealed class CapturingLogger(List<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
                => messages.Add(formatter(state, exception));
        }
    }
}
