using System;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Settings.Tests;

/// <summary>
/// FDW-4599: whether founding makes a default workspace is the settings row, then the value the option declares (true, what founding always did), and a name declared nowhere is a
/// coded failure, never a stand-in.
/// </summary>
public sealed class WorkspaceFoundingPolicyTests
{
    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    [InlineData(true)]
    [InlineData(false)]
    public void ASettingRowStatesTheValueAndWinsOverTheDeclaredDefault(bool stated)
    {
        var policy = new WorkspaceFoundingPolicy(new SettingsDouble { Stated = stated }, NullLogger<WorkspaceFoundingPolicy>.Instance);

        var result = policy.FoundingMakesDefaultWorkspace();

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(stated);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void WithoutARowTheDeclaredDefaultAppliesAndItIsThatFoundingMakesOne()
    {
        var policy = new WorkspaceFoundingPolicy(new SettingsDouble(), NullLogger<WorkspaceFoundingPolicy>.Instance);

        var result = policy.FoundingMakesDefaultWorkspace();

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TheSettingIsDefinedAsABooleanUnderTheNameTheOptionDeclares()
    {
        SettingDefinitions.FoundingMakesDefaultWorkspace.ShouldBe(WorkspaceFoundingOption.FoundingMakesDefaultWorkspace);
        SettingDefinitions.TryGet(SettingDefinitions.FoundingMakesDefaultWorkspace, out var definition).ShouldBeTrue();
        definition.DataType.ShouldBe("Boolean");
        WorkspaceFoundingOption.Instance.DeclaredDefaults[WorkspaceFoundingOption.FoundingMakesDefaultWorkspace].ShouldBe(true);
    }

    // A settings provider with one stated value, or none.
    private sealed class SettingsDouble : IEffectiveSettingsProvider
    {
        public bool? Stated { get; set; }

        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null)
            => throw new NotSupportedException("The policy reads through TryGetEffectiveValue.");

        public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
        {
            if (Stated is { } stated && stated is T typed)
            {
                value = typed;
                return true;
            }

            value = default!;
            return false;
        }
    }
}
