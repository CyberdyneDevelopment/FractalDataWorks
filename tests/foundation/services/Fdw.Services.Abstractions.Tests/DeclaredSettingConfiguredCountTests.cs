using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.Abstractions.Tests;

/// <summary>
/// FDW-7103: a host-wide limit a type declares is read at Register from the host's own configuration. A value the host states at
/// <c>{section}:{field}</c> wins; with none, the host's override applies, else the declared default; text that is not a whole number, or one below one,
/// refuses and names the key. Each test uses its own option type, because overrides are held per type for the process.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public sealed class DeclaredSettingConfiguredCountTests
{
    private const string Section = "Stub:Default";
    private const string Field = "MaxRows";

    private abstract class LimitTypeStub : ITypeOption, IDeclaresDefaults
    {
        public string Name => GetType().Name;

        public string Category => GetType().Name;

        public IReadOnlyDictionary<string, object> DeclaredDefaults { get; } =
            new Dictionary<string, object>(StringComparer.Ordinal) { [Field] = 250 };
    }

    private sealed class DeclaringType : LimitTypeStub
    {
    }

    private sealed class OverriddenType : LimitTypeStub
    {
    }

    private sealed class StatedOverOverrideType : LimitTypeStub
    {
    }

    private static HostApplicationBuilder Builder(string? stated)
    {
        var builder = Host.CreateApplicationBuilder();
        if (stated is not null)
            builder.Configuration[$"{Section}:{Field}"] = stated;

        return builder;
    }

    [Fact]
    public void TheDeclaredDefaultAppliesWhenTheHostStatesNone()
    {
        var resolved = DeclaredSetting.ConfiguredCount(Builder(null), Section, new DeclaringType(), Field, null);

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value.ShouldBe(250);
    }

    [Fact]
    public void AValueTheHostStatesWins()
    {
        var resolved = DeclaredSetting.ConfiguredCount(Builder("5000"), Section, new DeclaringType(), Field, null);

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value.ShouldBe(5000);
    }

    [Fact]
    public void TheHostsOverrideAppliesWhenItsConfigurationStatesNone()
    {
        ServiceTypeDefaults.Override(new OverriddenType(), Field, 40);

        var resolved = DeclaredSetting.ConfiguredCount(Builder(null), Section, new OverriddenType(), Field, null);

        resolved.Value.ShouldBe(40);
    }

    [Fact]
    public void AStatedValueWinsOverTheHostsOverride()
    {
        ServiceTypeDefaults.Override(new StatedOverOverrideType(), Field, 40);

        var resolved = DeclaredSetting.ConfiguredCount(Builder("70"), Section, new StatedOverOverrideType(), Field, null);

        resolved.Value.ShouldBe(70);
    }

    [Theory]
    [InlineData("lots", "ABSTRACTIONS3-61037")]
    [InlineData("1.5", "ABSTRACTIONS3-61037")]
    [InlineData("0", "ABSTRACTIONS3-61036")]
    [InlineData("-3", "ABSTRACTIONS3-61036")]
    public void AStatedValueThatIsNotAWholeNumberOfAtLeastOneIsRefusedNamingTheKey(string stated, string code)
    {
        var resolved = DeclaredSetting.ConfiguredCount(Builder(stated), Section, new DeclaringType(), Field, null);

        resolved.IsSuccess.ShouldBeFalse();
        resolved.Messages.ShouldContain(m => m.Code == code);
        resolved.CurrentMessage!.ShouldContain($"{Section}:{Field}");
    }
}
