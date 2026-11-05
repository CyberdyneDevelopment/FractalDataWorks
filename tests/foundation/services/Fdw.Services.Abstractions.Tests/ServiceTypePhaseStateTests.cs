using System;
using Fdw.ServiceTypes;
using Shouldly;
using Xunit;

namespace Fdw.Services.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="ServiceTypePhaseState"/> — the run-state registry that gives the three-phase
/// pipeline its idempotence + order control at collection and option granularity, keyed per scope.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public sealed class ServiceTypePhaseStateTests
{
    [Fact]
    public void TryMarkCollectionReturnsTrueFirstThenFalseForSameScopeAndPhase()
    {
        var scope = new object();

        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhases.Register).ShouldBeTrue();
        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhases.Register).ShouldBeFalse();
    }

    [Fact]
    public void TryMarkCollectionTracksEachPhaseIndependently()
    {
        var scope = new object();

        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhases.Configure).ShouldBeTrue();
        // Marking Configure must NOT mark Register/Initialize.
        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhases.Register).ShouldBeTrue();
        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhases.Initialize).ShouldBeTrue();
    }

    [Fact]
    public void TryMarkCollectionIsolatesScopes()
    {
        var scopeA = new object();
        var scopeB = new object();

        ServiceTypePhaseState.TryMarkCollection("Widgets", scopeA, ServiceTypePhases.Register).ShouldBeTrue();
        // A different scope (e.g. a second host/test container) gets a full run.
        ServiceTypePhaseState.TryMarkCollection("Widgets", scopeB, ServiceTypePhases.Register).ShouldBeTrue();
    }

    [Fact]
    public void TryMarkCollectionTracksEachCategoryIndependently()
    {
        var scope = new object();

        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhases.Register).ShouldBeTrue();
        ServiceTypePhaseState.TryMarkCollection("Gadgets", scope, ServiceTypePhases.Register).ShouldBeTrue();
    }

    [Fact]
    public void TryMarkOptionReturnsTrueFirstThenFalseForSameScopePhaseAndOption()
    {
        var scope = new object();

        ServiceTypePhaseState.TryMarkOption("Widgets", "Foo", scope, ServiceTypePhases.Register).ShouldBeTrue();
        ServiceTypePhaseState.TryMarkOption("Widgets", "Foo", scope, ServiceTypePhases.Register).ShouldBeFalse();
    }

    [Fact]
    public void TryMarkOptionTracksEachOptionIndependently()
    {
        var scope = new object();

        ServiceTypePhaseState.TryMarkOption("Widgets", "Foo", scope, ServiceTypePhases.Register).ShouldBeTrue();
        ServiceTypePhaseState.TryMarkOption("Widgets", "Bar", scope, ServiceTypePhases.Register).ShouldBeTrue();
    }

    [Fact]
    public void CollectionAndOptionMarksDoNotAliasEachOther()
    {
        var scope = new object();

        // The collection's whole-phase mark (option == null) and an option mark are distinct keys.
        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhases.Register).ShouldBeTrue();
        ServiceTypePhaseState.TryMarkOption("Widgets", "Foo", scope, ServiceTypePhases.Register).ShouldBeTrue();
    }

    [Fact]
    public void RunningAheadOfThePassMakesThePassSkip_OrderControl()
    {
        var scope = new object();

        // A caller runs a collection's phase early (to control order) → marks it.
        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhases.Initialize).ShouldBeTrue();

        // The later pass sees it already ran and skips.
        ServiceTypePhaseState.HasCollectionRun("Widgets", scope, ServiceTypePhases.Initialize).ShouldBeTrue();
        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhases.Initialize).ShouldBeFalse();
    }

    [Fact]
    public void HasCollectionRunIsFalseBeforeAnyMark()
    {
        var scope = new object();
        ServiceTypePhaseState.HasCollectionRun("Widgets", scope, ServiceTypePhases.Configure).ShouldBeFalse();
    }

    [Fact]
    public void TryMarkCollectionFailsLoudOnNullScope()
        => Should.Throw<ArgumentNullException>(() =>
            ServiceTypePhaseState.TryMarkCollection("Widgets", null!, ServiceTypePhases.Register));

    [Fact]
    public void TryMarkCollectionFailsLoudOnEmptyCategory()
        => Should.Throw<ArgumentException>(() =>
            ServiceTypePhaseState.TryMarkCollection("", new object(), ServiceTypePhases.Register));

    [Fact]
    public void TryMarkOptionFailsLoudOnEmptyOptionName()
        => Should.Throw<ArgumentException>(() =>
            ServiceTypePhaseState.TryMarkOption("Widgets", "", new object(), ServiceTypePhases.Register));
}
