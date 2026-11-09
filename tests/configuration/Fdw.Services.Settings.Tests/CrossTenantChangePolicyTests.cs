using System;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Settings.Tests;

/// <summary>
/// FDW-4459: whether an administrator of one tenant may change another tenant's object is configuration. A settings row states it; without one the value the option declares
/// (or the host overrides) applies; a change inside the caller's own tenant reads no setting; and a value stated nowhere is a coded failure.
/// </summary>
public sealed class CrossTenantChangePolicyTests
{
    private static readonly Guid Owner = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Other = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(CrossTenantChangeOption.ConnectionEnable)]
    [InlineData(CrossTenantChangeOption.DataStoreWrite)]
    public void ASettingRowThatIsOnAllowsAnotherTenantsAdministrator(string change)
    {
        var policy = new CrossTenantChangePolicy(new SettingsDouble { Stated = true }, NullLogger<CrossTenantChangePolicy>.Instance);

        policy.Permit(change, Owner, Other).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(CrossTenantChangeOption.ConnectionEnable)]
    [InlineData(CrossTenantChangeOption.DataStoreWrite)]
    public void ASettingRowThatIsOffRefusesAnotherTenantsAdministratorAndTheRefusalNamesTheSetting(string change)
    {
        var policy = new CrossTenantChangePolicy(new SettingsDouble { Stated = false }, NullLogger<CrossTenantChangePolicy>.Instance);

        var result = policy.Permit(change, Owner, Other);

        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsForbidden(result).ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(change);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(CrossTenantChangeOption.ConnectionEnable, true)]
    [InlineData(CrossTenantChangeOption.DataStoreWrite, false)]
    public void WithoutARowTheValueTheOptionDeclaresApplies(string change, bool allowed)
    {
        var policy = new CrossTenantChangePolicy(new SettingsDouble(), NullLogger<CrossTenantChangePolicy>.Instance);

        policy.Permit(change, Owner, Other).IsSuccess.ShouldBe(allowed);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASettingNoRowStatesAndNoOptionDeclaresIsACodedFailureAndNeverAStandInValue()
    {
        var policy = new CrossTenantChangePolicy(new SettingsDouble(), NullLogger<CrossTenantChangePolicy>.Instance);

        var result = policy.Permit("NoSuchChange", Owner, Other);

        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsForbidden(result).ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("NoSuchChange");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AChangeInTheCallersOwnTenantReadsNoSetting()
    {
        var settings = new SettingsDouble { Stated = false };
        var policy = new CrossTenantChangePolicy(settings, NullLogger<CrossTenantChangePolicy>.Instance);

        policy.Permit(CrossTenantChangeOption.DataStoreWrite, Owner, Owner).IsSuccess.ShouldBeTrue();

        settings.Reads.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ACallerWithNoActiveTenantIsRefusedAndNeverTreatedAsTheOwner()
    {
        var policy = new CrossTenantChangePolicy(new SettingsDouble { Stated = true }, NullLogger<CrossTenantChangePolicy>.Instance);

        var result = policy.Permit(CrossTenantChangeOption.ConnectionEnable, Owner, null);

        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsForbidden(result).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TheSettingsAreDefinedAsBooleansUnderTheNamesTheOptionDeclares()
    {
        SettingDefinitions.CrossTenantConnectionEnable.ShouldBe(CrossTenantChangeOption.ConnectionEnable);
        SettingDefinitions.CrossTenantDataStoreWrite.ShouldBe(CrossTenantChangeOption.DataStoreWrite);
        CrossTenantChangeOption.Instance.DeclaredDefaults.Keys.ShouldBe([CrossTenantChangeOption.ConnectionEnable, CrossTenantChangeOption.DataStoreWrite], ignoreOrder: true);
    }

    // A settings provider with one stated value for every name, or none.
    private sealed class SettingsDouble : IEffectiveSettingsProvider
    {
        public bool? Stated { get; set; }

        public System.Collections.Generic.List<string> Reads { get; } = [];

        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null)
            => throw new NotSupportedException("The policy reads through TryGetEffectiveValue.");

        public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
        {
            Reads.Add(settingName);
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
