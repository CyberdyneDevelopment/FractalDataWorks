using System;
using System.Linq;
using Fdw.Services.RateLimiting.Abstractions;

namespace Fdw.Services.RateLimiting.Abstractions.Tests;

/// <summary>
/// Tests for the settings every policy's allowance is read from, and the one place their names are made (FDW-1945).
/// </summary>
public class RateLimitAllowanceFieldsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ASettingIsNamedForItsPolicyAndItsField()
    {
        RateLimitAllowanceFields.SettingName("Authenticated", RateLimitAllowanceFields.RequestsPerWindow)
            .ShouldBe("RateLimit.Authenticated.RequestsPerWindow");
        RateLimitAllowanceFields.SettingName("Standard", RateLimitAllowanceFields.Partitioning)
            .ShouldBe("RateLimit.Standard.Partitioning");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EveryFieldHasItsOwnNameAndAKindTheSettingsHold()
    {
        var all = RateLimitAllowanceFields.All;

        all.Select(f => f.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(all.Count);
        all.ShouldAllBe(f => f.DataType == "Int32" || f.DataType == "Boolean" || f.DataType == "String");
        all.ShouldAllBe(f => f.Description.Length > 0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EveryMemberOfAnAllowanceHasAField()
    {
        // RateLimitAllowance has nine members that are read from settings; a tenth without a field would be a number no setting sets.
        RateLimitAllowanceFields.All.Count.ShouldBe(9);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SettingNameThrowsOnNullArguments()
    {
        Should.Throw<ArgumentNullException>(() => RateLimitAllowanceFields.SettingName(null!, RateLimitAllowanceFields.QueueLimit));
        Should.Throw<ArgumentNullException>(() => RateLimitAllowanceFields.SettingName("Admin", null!));
    }
}
