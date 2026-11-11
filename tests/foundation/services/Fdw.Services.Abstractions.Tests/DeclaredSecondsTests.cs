using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.ServiceTypes;

namespace Fdw.Services.Abstractions.Tests;

/// <summary>
/// FDW-4904: a timeout a configuration may leave out is declared on its type and changed by a host at registration; the value the configuration
/// states wins over both, in the factory. Each test uses its own option type, because overrides are held per type for the process.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public sealed class DeclaredSecondsTests
{
    private const string Field = "CommandTimeoutSeconds";

    private abstract class TimeoutTypeStub : ITypeOption, IDeclaresDefaults
    {
        public string Name => GetType().Name;

        public abstract string Category { get; }

        public virtual IReadOnlyDictionary<string, object> DeclaredDefaults { get; } =
            new Dictionary<string, object>(StringComparer.Ordinal) { [Field] = 30 };
    }

    private sealed class DeclaringType : TimeoutTypeStub
    {
        public override string Category => "TimeoutStubDeclaring";
    }

    private sealed class OverriddenType : TimeoutTypeStub
    {
        public override string Category => "TimeoutStubOverridden";
    }

    private sealed class CategoryType : TimeoutTypeStub
    {
        public override string Category => "TimeoutStubCategory";
    }

    private sealed class ZeroOverrideType : TimeoutTypeStub
    {
        public override string Category => "TimeoutStubZero";
    }

    private sealed class SilentType : TimeoutTypeStub
    {
        public override string Category => "TimeoutStubSilent";

        public override IReadOnlyDictionary<string, object> DeclaredDefaults { get; } = new Dictionary<string, object>(StringComparer.Ordinal);
    }

    [Fact]
    public void TheDeclaredDefaultApplies()
    {
        var resolved = DeclaredSeconds.Resolve(new DeclaringType(), Field, null);

        resolved.IsSuccess.ShouldBeTrue();
        resolved.Value.ShouldBe(30);
    }

    [Fact]
    public void ARegistrationOverrideWinsOverTheDeclaredDefault()
    {
        var type = new OverriddenType();
        ServiceTypeDefaults.Override(type, Field, 90);

        var resolved = DeclaredSeconds.Resolve(type, Field, null);

        resolved.IsSuccess.ShouldBeTrue();
        resolved.Value.ShouldBe(90);
    }

    [Fact]
    public void ACategoryOverrideWinsOverTheDeclaredDefault()
    {
        ServiceTypeDefaults.OverrideForCategory("TimeoutStubCategory", Field, 45);

        DeclaredSeconds.Resolve(new CategoryType(), Field, null).Value.ShouldBe(45);
    }

    [Fact]
    public void ATypeThatDeclaresNoDefaultIsRefusedNamingTheField()
    {
        var resolved = DeclaredSeconds.Resolve(new SilentType(), Field, null);

        resolved.IsFailure.ShouldBeTrue();
        resolved.Messages[^1].Message.ShouldContain(Field);
    }

    [Fact]
    public void AnOverrideBelowOneSecondIsRefused()
    {
        var type = new ZeroOverrideType();
        ServiceTypeDefaults.Override(type, Field, 0);

        DeclaredSeconds.Resolve(type, Field, null).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void AValueTheConnectionStatesWinsOverTheTypesDefault()
    {
        var effective = DeclaredSeconds.Effective(600, 30, "MsSql", Field, null);

        effective.IsSuccess.ShouldBeTrue();
        effective.Value.ShouldBe(600);
    }

    [Fact]
    public void AConnectionThatStatesNoneTakesTheTypesDefault()
        => DeclaredSeconds.Effective(null, 30, "MsSql", Field, null).Value.ShouldBe(30);

    [Fact]
    public void AConnectionThatStatesNoneFromAFactoryHandedNoDefaultIsRefused()
    {
        var effective = DeclaredSeconds.Effective(null, null, "MsSql", Field, null);

        effective.IsFailure.ShouldBeTrue();
        effective.Messages[^1].Message.ShouldContain("MsSql");
    }

    [Fact]
    public void AConnectionThatStatesATimeoutBelowOneSecondIsRefused()
    {
        var effective = DeclaredSeconds.Effective(0, 30, "MsSql", Field, null);

        effective.IsFailure.ShouldBeTrue();
        effective.Messages[^1].Code.ShouldBe("ABSTRACTIONS3-61031");
    }
}
