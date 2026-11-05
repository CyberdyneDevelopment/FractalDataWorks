using System;
using System.Linq;
using Fdw.Services.Scheduling.Abstractions.OptionTypes;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.OptionTypes;

/// <summary>
/// Tests for <see cref="MissedFireRules"/> (FDW-3788): a schedule states one of two rules for the cron fire times that passed while the scheduler was down, and a name that is
/// neither is not found.
/// </summary>
public class MissedFireRulesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void SkipDropsTheMissedFireTimes()
    {
        var rule = MissedFireRules.ByName("Skip");

        rule.Name.ShouldBe("Skip");
        rule.FiresOnce.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void FireOnceRunsTheScheduleOnceForTheMissedFireTimes()
    {
        var rule = MissedFireRules.ByName("FireOnce");

        rule.Name.ShouldBe("FireOnce");
        rule.FiresOnce.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Scheduling")]
    public void ThereAreTwoRulesAndAnyOtherNameIsNotFound()
    {
        MissedFireRules.All().Select(rule => rule.Name).OrderBy(name => name, StringComparer.Ordinal).ToList().ShouldBe(["FireOnce", "Skip"]);
        ReferenceEquals(MissedFireRules.ByName("Catchup"), MissedFireRules.NotFound).ShouldBeTrue();
        ReferenceEquals(MissedFireRules.ByName(string.Empty), MissedFireRules.NotFound).ShouldBeTrue();
    }
}
