using System.Collections.Generic;
using System.Linq;
using Fdw.UI.ComponentTypeOptions;
using Shouldly;
using Xunit;

namespace Fdw.UI.Tests.ComponentTypeOptions;

/// <summary>
/// Tests the assembly set a host hands to Blazor's component discovery.
/// </summary>
/// <remarks>
/// This is where a component mechanism differs from an endpoint one, and both properties here are
/// load-bearing. Blazor throws "Assembly already defined" on a duplicate, so a set that is not
/// distinct crashes the host at startup — several components in one package is the normal case, not
/// an edge one. And a skipped component whose assembly still reaches discovery is still findable,
/// which would make SkipRegistration decorative on this side.
/// </remarks>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public sealed class ComponentAssembliesTests
{
    private sealed class FirstProvider;

    private sealed class SecondProvider;

    private sealed class TestComponentOption(System.Type componentType, string name)
        : ComponentTypeOptionBase(name, componentType, $"The {name} component.", "Test");

    private sealed class TestCollection(IEnumerable<IComponentTypeOption> members)
        : ComponentTypeCollectionBase<ComponentTypeOptionBase>
    {
        public override IEnumerable<IComponentTypeOption> Members { get; } = members;
    }

    /// <summary>Two components from one assembly yield that assembly once.</summary>
    [Fact]
    public void AssembliesAreDistinct()
    {
        var collection = new TestCollection(
        [
            new TestComponentOption(typeof(FirstProvider), "First"),
            new TestComponentOption(typeof(SecondProvider), "Second"),
        ]);

        collection.ComponentAssemblies.Count().ShouldBe(1);
    }

    /// <summary>A skipped component contributes no assembly.</summary>
    [Fact]
    public void SkippedComponentIsExcluded()
    {
        var collection = new TestCollection(
        [
            new TestComponentOption(typeof(FirstProvider), "First") { SkipRegistration = true },
        ]);

        collection.ComponentAssemblies.ShouldBeEmpty();
    }

    /// <summary>A skipped collection contributes no assemblies.</summary>
    [Fact]
    public void SkippedCollectionContributesNothing()
    {
        var collection = new TestCollection([new TestComponentOption(typeof(FirstProvider), "First")])
        {
            SkipRegistration = true,
        };

        collection.ComponentAssemblies.ShouldBeEmpty();
    }

    /// <summary>An empty collection yields no assemblies rather than throwing.</summary>
    [Fact]
    public void EmptyCollectionYieldsNoAssemblies()
    {
        new TestCollection([]).ComponentAssemblies.ShouldBeEmpty();
    }

    /// <summary>A declared component contributes the assembly it lives in.</summary>
    [Fact]
    public void DeclaredComponentContributesItsAssembly()
    {
        var collection = new TestCollection([new TestComponentOption(typeof(FirstProvider), "First")]);

        collection.ComponentAssemblies.ShouldContain(typeof(FirstProvider).Assembly);
    }
}
