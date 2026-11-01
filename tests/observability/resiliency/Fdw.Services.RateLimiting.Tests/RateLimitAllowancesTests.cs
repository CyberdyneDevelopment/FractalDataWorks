using System;
using Fdw.Services.RateLimiting.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.RateLimiting.Tests;

/// <summary>
/// Tests that a policy's allowance is read from settings, and that a setting that cannot be used is a refusal naming it and
/// never a number made up in its place (FDW-1945).
/// </summary>
public sealed class RateLimitAllowancesTests
{
    private static Fdw.Results.IGenericResult<RateLimitAllowance> ReadAuthenticated(FakeEffectiveSettings settings, Guid? company = null)
        => RateLimitAllowances.Read(settings, RateLimitPolicies.Authenticated, company, NullLogger.Instance);

    private static string NameOf(string policy, RateLimitAllowanceField field)
        => RateLimitAllowanceFields.SettingName(policy, field);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EveryTierReadsTheNumbersItWasHeldToBeforeTheyWereSettings()
    {
        var settings = TodaysRateLimitSettings.All();

        foreach (var tier in TodaysRateLimitSettings.Tiers)
        {
            var result = RateLimitAllowances.Read(settings, RateLimitPolicies.ByName(tier.Policy), null, NullLogger.Instance);

            result.IsSuccess.ShouldBeTrue(tier.Policy);
            result.Value!.RequestsPerWindow.ShouldBe(tier.Requests);
            result.Value.Window.ShouldBe(TimeSpan.FromSeconds(tier.Seconds));
            result.Value.AllowBurst.ShouldBe(tier.Burst);
            result.Value.BurstLimit.ShouldBe(tier.BurstLimit);
            result.Value.Algorithm.Name.ShouldBe(tier.Algorithm);
            result.Value.SegmentsPerWindow.ShouldBe(tier.Segments);
            result.Value.QueueExceededRequests.ShouldBeFalse();
            result.Value.QueueLimit.ShouldBe(0);
            result.Value.Partitioning.Name.ShouldBe(tier.Partitioning);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ASettingChangedIsWhatTheNextReadReturns()
    {
        var settings = TodaysRateLimitSettings.All();
        ReadAuthenticated(settings).Value!.RequestsPerWindow.ShouldBe(500);

        TodaysRateLimitSettings.Set(settings, "Authenticated", RateLimitAllowanceFields.RequestsPerWindow, "2500");

        ReadAuthenticated(settings).Value!.RequestsPerWindow.ShouldBe(2500);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EverySettingThatIsNotSetIsARefusalNamingIt()
    {
        foreach (var field in RateLimitAllowanceFields.All)
        {
            var settings = TodaysRateLimitSettings.All().Remove(NameOf("Authenticated", field));

            var result = ReadAuthenticated(settings);

            result.IsFailure.ShouldBeTrue(field.Name);
            result.CurrentMessage.ShouldNotBeNull();
            result.CurrentMessage.ShouldContain(NameOf("Authenticated", field));
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AWordWhereANumberGoesIsARefusalNamingTheSetting()
    {
        var settings = TodaysRateLimitSettings.All();
        TodaysRateLimitSettings.Set(settings, "Authenticated", RateLimitAllowanceFields.RequestsPerWindow, "lots");

        var result = ReadAuthenticated(settings);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldContain(NameOf("Authenticated", RateLimitAllowanceFields.RequestsPerWindow));
    }

    [Theory]
    [InlineData("RequestsPerWindow", "0")]
    [InlineData("RequestsPerWindow", "-5")]
    [InlineData("WindowSeconds", "0")]
    [InlineData("BurstLimit", "0")]
    [InlineData("SegmentsPerWindow", "0")]
    [InlineData("QueueLimit", "-1")]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ANumberTheLimiterCannotUseIsARefusalNamingTheSetting(string field, string value)
    {
        var settings = TodaysRateLimitSettings.All();
        settings.Set(RateLimitAllowanceFields.Prefix + ".Authenticated." + field, value);

        var result = ReadAuthenticated(settings);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldContain(RateLimitAllowanceFields.Prefix + ".Authenticated." + field);
        result.CurrentMessage.ShouldContain("'" + value + "'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ANameThatIsNotAnAlgorithmIsARefusalNamingTheSetting()
    {
        var settings = TodaysRateLimitSettings.All();
        TodaysRateLimitSettings.Set(settings, "Authenticated", RateLimitAllowanceFields.Algorithm, "Leaky");

        var result = ReadAuthenticated(settings);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldContain(NameOf("Authenticated", RateLimitAllowanceFields.Algorithm));
        result.CurrentMessage.ShouldContain("'Leaky'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ANameThatIsNotAWayOfCountingCallersIsARefusalNamingTheSetting()
    {
        var settings = TodaysRateLimitSettings.All();
        TodaysRateLimitSettings.Set(settings, "Authenticated", RateLimitAllowanceFields.Partitioning, "PerMoon");

        var result = ReadAuthenticated(settings);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldContain(NameOf("Authenticated", RateLimitAllowanceFields.Partitioning));
        result.CurrentMessage.ShouldContain("'PerMoon'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void OnlyTheFirstSettingThatCannotBeUsedIsNamed()
    {
        var settings = TodaysRateLimitSettings.All();
        TodaysRateLimitSettings.Set(settings, "Authenticated", RateLimitAllowanceFields.RequestsPerWindow, "0");
        TodaysRateLimitSettings.Set(settings, "Authenticated", RateLimitAllowanceFields.WindowSeconds, "0");

        var result = ReadAuthenticated(settings);

        result.CurrentMessage.ShouldContain(NameOf("Authenticated", RateLimitAllowanceFields.RequestsPerWindow));
        result.CurrentMessage.ShouldNotContain(NameOf("Authenticated", RateLimitAllowanceFields.WindowSeconds));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ReadThrowsOnNullArguments()
    {
        var settings = TodaysRateLimitSettings.All();

        Should.Throw<ArgumentNullException>(() => RateLimitAllowances.Read(null!, RateLimitPolicies.Authenticated, null, NullLogger.Instance));
        Should.Throw<ArgumentNullException>(() => RateLimitAllowances.Read(settings, null!, null, NullLogger.Instance));
        Should.Throw<ArgumentNullException>(() => RateLimitAllowances.Read(settings, RateLimitPolicies.Authenticated, null, null!));
    }

    // ---- a company's own allowance (FDW-1946) -------------------------------------------------------------------------

    private static readonly Guid CompanyA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CompanyB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static string Requests => NameOf("Authenticated", RateLimitAllowanceFields.RequestsPerWindow);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ACompanyWithItsOwnNumberIsHeldToItAndAnotherCompanyIsNot()
    {
        var settings = TodaysRateLimitSettings.All().SetForCompany(CompanyA, Requests, "2500");

        ReadAuthenticated(settings, CompanyA).Value!.RequestsPerWindow.ShouldBe(2500);
        ReadAuthenticated(settings, CompanyB).Value!.RequestsPerWindow.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ARequestThatBelongsToNoCompanyIsHeldToTheServersNumber()
    {
        var settings = TodaysRateLimitSettings.All().SetForCompany(CompanyA, Requests, "2500");

        ReadAuthenticated(settings, null).Value!.RequestsPerWindow.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ClearingACompanysNumberReturnsItToTheServers()
    {
        var settings = TodaysRateLimitSettings.All().SetForCompany(CompanyA, Requests, "2500");
        ReadAuthenticated(settings, CompanyA).Value!.RequestsPerWindow.ShouldBe(2500);

        settings.ClearForCompany(CompanyA, Requests);

        ReadAuthenticated(settings, CompanyA).Value!.RequestsPerWindow.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ACompanysOwnNumberChangesOnlyThatSettingAndTheRestComeFromTheServer()
    {
        var settings = TodaysRateLimitSettings.All().SetForCompany(CompanyA, Requests, "2500");

        var allowance = ReadAuthenticated(settings, CompanyA).Value!;

        allowance.RequestsPerWindow.ShouldBe(2500);
        allowance.BurstLimit.ShouldBe(750);
        allowance.Algorithm.Name.ShouldBe("SlidingWindow");
        allowance.Partitioning.Name.ShouldBe("PerCaller");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ALockedServerValueIsHeldByEveryCompany()
    {
        var settings = TodaysRateLimitSettings.All().SetForCompany(CompanyA, Requests, "2500").Lock(Requests);

        ReadAuthenticated(settings, CompanyA).Value!.RequestsPerWindow.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ACompanysNumberTheLimiterCannotUseIsARefusalNamingTheSetting()
    {
        var settings = TodaysRateLimitSettings.All().SetForCompany(CompanyA, Requests, "0");

        var result = ReadAuthenticated(settings, CompanyA);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldContain(Requests);
        ReadAuthenticated(settings, CompanyB).IsSuccess.ShouldBeTrue();
    }
}
