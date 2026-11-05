using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.ServiceTypes;

namespace Fdw.Services.Abstractions.Tests;

/// <summary>
/// FDW-3956: a type option that is not a service type (a data set kind, an ETL row mapper, a real-time hub) declares tuning defaults and
/// a host overrides them, through the same <see cref="ServiceTypeDefaults"/> a service type uses. Each test uses its own option type,
/// because overrides are held per type for the process.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public sealed class ServiceTypeDefaultsTypeOptionTests
{
    private abstract class OptionStub : ITypeOption, IDeclaresDefaults
    {
        public string Name => GetType().Name;

        public abstract string Category { get; }

        public IReadOnlyDictionary<string, object> DeclaredDefaults { get; } =
            new Dictionary<string, object>(StringComparer.Ordinal) { ["PoolSize"] = 10 };
    }

    private sealed class DeclaringOption : OptionStub
    {
        public override string Category => "StubDeclaring";
    }

    private sealed class OverriddenOption : OptionStub
    {
        public override string Category => "StubOverridden";
    }

    private sealed class CategoryOption : OptionStub
    {
        public override string Category => "StubCategory";
    }

    private sealed class UncategorisedOption : OptionStub
    {
        public override string Category => string.Empty;
    }

    [Fact]
    public void ATypeOptionThatIsNotAServiceTypeGivesItsDeclaredValue()
    {
        ServiceTypeDefaults.TryResolve(new DeclaringOption(), "PoolSize", out var value).ShouldBeTrue();
        value.ShouldBe(10);
    }

    [Fact]
    public void AFieldTheOptionDoesNotDeclareResolvesToNothing()
    {
        ServiceTypeDefaults.TryResolve(new DeclaringOption(), "Unknown", out var value).ShouldBeFalse();
        value.ShouldBeNull();
    }

    [Fact]
    public void AHostOverrideWinsOverTheDeclaredValue()
    {
        var option = new OverriddenOption();
        ServiceTypeDefaults.Override(option, "PoolSize", 25);

        ServiceTypeDefaults.TryResolve(option, "PoolSize", out var value).ShouldBeTrue();
        value.ShouldBe(25);
    }

    [Fact]
    public void ACategoryOverrideWinsOverTheDeclaredValue()
    {
        ServiceTypeDefaults.OverrideForCategory("StubCategory", "PoolSize", 40);

        ServiceTypeDefaults.TryResolve(new CategoryOption(), "PoolSize", out var value).ShouldBeTrue();
        value.ShouldBe(40);
    }

    [Fact]
    public void AnOptionWithNoCategoryTakesNoCategoryOverride()
    {
        // No key is guessed for an option that states no category; it gives its own declared value.
        ServiceTypeDefaults.OverrideForCategory("Elsewhere", "PoolSize", 99);

        ServiceTypeDefaults.TryResolve(new UncategorisedOption(), "PoolSize", out var value).ShouldBeTrue();
        value.ShouldBe(10);
    }
}
