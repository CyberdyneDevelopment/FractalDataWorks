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

        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhaseState.Phase.Register).ShouldBeTrue();
        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhaseState.Phase.Register).ShouldBeFalse();
    }

    [Fact]
    public void TryMarkCollectionTracksEachPhaseIndependently()
    {
        var scope = new object();

        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhaseState.Phase.Configure).ShouldBeTrue();
        // Marking Configure must NOT mark Register/Initialize.
        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhaseState.Phase.Register).ShouldBeTrue();
        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhaseState.Phase.Initialize).ShouldBeTrue();
    }

    [Fact]
    public void TryMarkCollectionIsolatesScopes()
    {
        var scopeA = new object();
        var scopeB = new object();

        ServiceTypePhaseState.TryMarkCollection("Widgets", scopeA, ServiceTypePhaseState.Phase.Register).ShouldBeTrue();
        // A different scope (e.g. a second host/test container) gets a full run.
        ServiceTypePhaseState.TryMarkCollection("Widgets", scopeB, ServiceTypePhaseState.Phase.Register).ShouldBeTrue();
    }

    [Fact]
    public void TryMarkCollectionTracksEachCategoryIndependently()
    {
        var scope = new object();

        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhaseState.Phase.Register).ShouldBeTrue();
        ServiceTypePhaseState.TryMarkCollection("Gadgets", scope, ServiceTypePhaseState.Phase.Register).ShouldBeTrue();
    }

    [Fact]
    public void TryMarkOptionReturnsTrueFirstThenFalseForSameScopePhaseAndOption()
    {
        var scope = new object();

        ServiceTypePhaseState.TryMarkOption("Widgets", "Foo", scope, ServiceTypePhaseState.Phase.Register).ShouldBeTrue();
        ServiceTypePhaseState.TryMarkOption("Widgets", "Foo", scope, ServiceTypePhaseState.Phase.Register).ShouldBeFalse();
    }

    [Fact]
    public void TryMarkOptionTracksEachOptionIndependently()
    {
        var scope = new object();

        ServiceTypePhaseState.TryMarkOption("Widgets", "Foo", scope, ServiceTypePhaseState.Phase.Register).ShouldBeTrue();
        ServiceTypePhaseState.TryMarkOption("Widgets", "Bar", scope, ServiceTypePhaseState.Phase.Register).ShouldBeTrue();
    }

    [Fact]
    public void CollectionAndOptionMarksDoNotAliasEachOther()
    {
        var scope = new object();

        // The collection's whole-phase mark (option == null) and an option mark are distinct keys.
        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhaseState.Phase.Register).ShouldBeTrue();
        ServiceTypePhaseState.TryMarkOption("Widgets", "Foo", scope, ServiceTypePhaseState.Phase.Register).ShouldBeTrue();
    }

    [Fact]
    public void RunningAheadOfTheSweepMakesTheSweepSkip_OrderControl()
    {
        var scope = new object();

        // A caller runs a collection's phase early (to control order) → marks it.
        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhaseState.Phase.Initialize).ShouldBeTrue();

        // The later sweep sees it already ran and skips.
        ServiceTypePhaseState.HasCollectionRun("Widgets", scope, ServiceTypePhaseState.Phase.Initialize).ShouldBeTrue();
        ServiceTypePhaseState.TryMarkCollection("Widgets", scope, ServiceTypePhaseState.Phase.Initialize).ShouldBeFalse();
    }

    [Fact]
    public void HasCollectionRunIsFalseBeforeAnyMark()
    {
        var scope = new object();
        ServiceTypePhaseState.HasCollectionRun("Widgets", scope, ServiceTypePhaseState.Phase.Configure).ShouldBeFalse();
    }

    [Fact]
    public void TryMarkCollectionFailsLoudOnNullScope()
        => Should.Throw<ArgumentNullException>(() =>
            ServiceTypePhaseState.TryMarkCollection("Widgets", null!, ServiceTypePhaseState.Phase.Register));

    [Fact]
    public void TryMarkCollectionFailsLoudOnEmptyCategory()
        => Should.Throw<ArgumentException>(() =>
            ServiceTypePhaseState.TryMarkCollection("", new object(), ServiceTypePhaseState.Phase.Register));

    [Fact]
    public void TryMarkOptionFailsLoudOnEmptyOptionName()
        => Should.Throw<ArgumentException>(() =>
            ServiceTypePhaseState.TryMarkOption("Widgets", "", new object(), ServiceTypePhaseState.Phase.Register));
}
