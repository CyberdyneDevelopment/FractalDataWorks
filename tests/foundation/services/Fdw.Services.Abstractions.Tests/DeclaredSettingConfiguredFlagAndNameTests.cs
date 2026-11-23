using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.Abstractions.Tests;

/// <summary>
/// FDW-6901: a host-wide true-or-false setting and a host-wide name setting a type declares are read at Register the way a limit is
/// (<see cref="DeclaredSetting.ConfiguredCount"/>): a value the host states at <c>{section}:{field}</c> wins; with none, the host's override
/// applies, else the declared default; a flag stated as neither true nor false refuses and names the key. Each test uses its own option type,
/// because overrides are held per type for the process.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public sealed class DeclaredSettingConfiguredFlagAndNameTests
{
    private const string Section = "Stub:Default";
    private const string FlagField = "ReadsInParallel";
    private const string NameField = "DefaultStrategy";

    private abstract class SettingTypeStub : ITypeOption, IDeclaresDefaults
    {
        public string Name => GetType().Name;

        public string Category => GetType().Name;

        public IReadOnlyDictionary<string, object> DeclaredDefaults { get; } =
            new Dictionary<string, object>(StringComparer.Ordinal) { [FlagField] = true, [NameField] = "Optimized" };
    }

    private sealed class DeclaringType : SettingTypeStub
    {
    }

    private sealed class FlagOverriddenType : SettingTypeStub
    {
    }

    private sealed class NameOverriddenType : SettingTypeStub
    {
    }

    private sealed class DeclaresNothingType : ITypeOption
    {
        public string Name => nameof(DeclaresNothingType);

        public string Category => nameof(DeclaresNothingType);
    }

    private static HostApplicationBuilder Builder(string field, string? stated)
    {
        var builder = Host.CreateApplicationBuilder();
        if (stated is not null)
            builder.Configuration[$"{Section}:{field}"] = stated;

        return builder;
    }

    [Fact]
    public void TheDeclaredFlagAppliesWhenTheHostStatesNone()
    {
        var resolved = DeclaredSetting.ConfiguredFlag(Builder(FlagField, null), Section, new DeclaringType(), FlagField, null);

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value.ShouldBeTrue();
    }

    [Fact]
    public void AFlagTheHostStatesWins()
    {
        var resolved = DeclaredSetting.ConfiguredFlag(Builder(FlagField, "false"), Section, new DeclaringType(), FlagField, null);

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value.ShouldBeFalse();
    }

    [Fact]
    public void TheHostsFlagOverrideAppliesWhenItsConfigurationStatesNone()
    {
        ServiceTypeDefaults.Override(new FlagOverriddenType(), FlagField, false);

        var resolved = DeclaredSetting.ConfiguredFlag(Builder(FlagField, null), Section, new FlagOverriddenType(), FlagField, null);

        resolved.Value.ShouldBeFalse();
    }

    [Fact]
    public void AFlagStatedAsNeitherTrueNorFalseIsRefusedNamingTheKey()
    {
        var resolved = DeclaredSetting.ConfiguredFlag(Builder(FlagField, "yes"), Section, new DeclaringType(), FlagField, null);

        resolved.IsSuccess.ShouldBeFalse();
        resolved.Messages.ShouldContain(m => m.Code == "ABSTRACTIONS3-61047");
        resolved.CurrentMessage!.ShouldContain($"{Section}:{FlagField}");
    }

    [Fact]
    public void TheDeclaredNameAppliesWhenTheHostStatesNoneAndAStatedOneWins()
    {
        DeclaredSetting.ConfiguredName(Builder(NameField, null), Section, new DeclaringType(), NameField, null).Value.ShouldBe("Optimized");
        DeclaredSetting.ConfiguredName(Builder(NameField, "Sequential"), Section, new DeclaringType(), NameField, null).Value.ShouldBe("Sequential");
    }

    [Fact]
    public void TheHostsNameOverrideAppliesWhenItsConfigurationStatesNone()
    {
        ServiceTypeDefaults.Override(new NameOverriddenType(), NameField, "Parallel");

        DeclaredSetting.ConfiguredName(Builder(NameField, null), Section, new NameOverriddenType(), NameField, null).Value.ShouldBe("Parallel");
    }

    [Fact]
    public void ANameNoOneDeclaresIsRefused()
    {
        var resolved = DeclaredSetting.ConfiguredName(Builder(NameField, null), Section, new DeclaresNothingType(), NameField, null);

        resolved.IsSuccess.ShouldBeFalse();
        resolved.Messages.ShouldContain(m => m.Code == "ABSTRACTIONS3-61048");
    }
}
