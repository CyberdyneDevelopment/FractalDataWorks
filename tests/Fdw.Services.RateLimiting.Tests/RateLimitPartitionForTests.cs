using System;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Fdw.Claims.Abstractions;
using Fdw.Services.RateLimiting.Abstractions;
using Fdw.Services.RateLimiting.Extensions;
using Fdw.Services.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Fdw.Services.RateLimiting.Tests;

/// <summary>
/// Tests that a policy's allowance is each caller's own (or each address's), not one that everyone under the policy shares, and
/// that the numbers and the way of counting are the policy's settings as they stand for each request (FDW-1945).
/// </summary>
public sealed class RateLimitPartitionForTests
{
    private readonly FakeEffectiveSettings _settings = TodaysRateLimitSettings.All();
    private readonly IServiceProvider _services;

    public RateLimitPartitionForTests()
    {
        _services = new ServiceCollection()
            .AddSingleton<IEffectiveSettingsProvider>(_settings)
            .AddLogging()
            .BuildServiceProvider();
        TodaysRateLimitSettings.Set(_settings, "Authenticated", RateLimitAllowanceFields.RequestsPerWindow, "5");
        TodaysRateLimitSettings.Set(_settings, "Standard", RateLimitAllowanceFields.RequestsPerWindow, "3");
    }

    private HttpContext CallerAt(string id, string address) => new DefaultHttpContext
    {
        RequestServices = _services,
        User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id) }, "test")),
        Connection = { RemoteIpAddress = IPAddress.Parse(address) },
    };

    private HttpContext CallerOfCompany(Guid company, string id, string address) => new DefaultHttpContext
    {
        RequestServices = _services,
        User = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id),
                new Claim(ClaimDefinitions.tenantId.Name, company.ToString()),
            },
            "test")),
        Connection = { RemoteIpAddress = IPAddress.Parse(address) },
    };

    private HttpContext At(string address) => new DefaultHttpContext
    {
        RequestServices = _services,
        Connection = { RemoteIpAddress = IPAddress.Parse(address) },
    };

    private static bool Acquired(PartitionedRateLimiter<HttpContext> limiter, HttpContext context)
    {
        using var lease = limiter.AttemptAcquire(context);
        return lease.IsAcquired;
    }

    // Uses up a caller's whole allowance, and says whether the next request was refused.
    private static bool RefusedAfterTheAllowance(PartitionedRateLimiter<HttpContext> limiter, Func<HttpContext> request, int allowance)
    {
        for (var used = 0; used < allowance; used++)
        {
            Acquired(limiter, request()).ShouldBeTrue();
        }

        return !Acquired(limiter, request());
    }

    private static PartitionedRateLimiter<HttpContext> LimiterFor(IRateLimitPolicy policy) =>
        PartitionedRateLimiter.Create<HttpContext, string>(context => RateLimitingServiceExtensions.PartitionFor(policy, context));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ACallerThatUsesItsAllowanceDoesNotRefuseAnotherCaller()
    {
        using var limiter = LimiterFor(RateLimitPolicies.Authenticated);

        RefusedAfterTheAllowance(limiter, () => CallerAt("busy", "10.0.0.1"), 5).ShouldBeTrue();
        Acquired(limiter, CallerAt("quiet", "10.0.0.1")).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AnAddressThatUsesItsAllowanceDoesNotRefuseAnotherAddress()
    {
        using var limiter = LimiterFor(RateLimitPolicies.Standard);

        RefusedAfterTheAllowance(limiter, () => At("10.0.0.1"), 3).ShouldBeTrue();
        Acquired(limiter, At("10.0.0.2")).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ASettingChangedHoldsFromTheNextRequest()
    {
        using var limiter = LimiterFor(RateLimitPolicies.Authenticated);
        RefusedAfterTheAllowance(limiter, () => CallerAt("busy", "10.0.0.1"), 5).ShouldBeTrue();

        TodaysRateLimitSettings.Set(_settings, "Authenticated", RateLimitAllowanceFields.RequestsPerWindow, "8");

        // The caller was refused under 5; under 8 it has its whole new allowance, and is refused after that.
        RefusedAfterTheAllowance(limiter, () => CallerAt("busy", "10.0.0.1"), 8).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TheWayOfCountingCallersFollowsItsSetting()
    {
        TodaysRateLimitSettings.Set(_settings, "Standard", RateLimitAllowanceFields.Partitioning, "Shared");
        using var limiter = LimiterFor(RateLimitPolicies.Standard);

        RefusedAfterTheAllowance(limiter, () => At("10.0.0.1"), 3).ShouldBeTrue();

        // Counted as one for everyone, another address is refused too.
        Acquired(limiter, At("10.0.0.2")).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ARequestWithNoCallerUnderAPerCallerPolicyIsAFaultNotASharedBucket()
    {
        Should.Throw<InvalidOperationException>(
            () => RateLimitingServiceExtensions.PartitionFor(
                RateLimitPolicies.Authenticated,
                new DefaultHttpContext { RequestServices = _services }));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ASettingThatIsNotSetIsAFaultNamingItNotAMadeUpAllowance()
    {
        _settings.Remove(RateLimitAllowanceFields.SettingName("Authenticated", RateLimitAllowanceFields.WindowSeconds));

        var fault = Should.Throw<InvalidOperationException>(
            () => RateLimitingServiceExtensions.PartitionFor(RateLimitPolicies.Authenticated, CallerAt("any", "10.0.0.1")));

        fault.Message.ShouldContain("RateLimit.Authenticated.WindowSeconds");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PartitionForThrowsOnNullArguments()
    {
        Should.Throw<ArgumentNullException>(() => RateLimitingServiceExtensions.PartitionFor(null!, new DefaultHttpContext()));
        Should.Throw<ArgumentNullException>(() => RateLimitingServiceExtensions.PartitionFor(RateLimitPolicies.Authenticated, null!));
    }

    // ---- a company's own allowance (FDW-1946) -------------------------------------------------------------------------

    private static readonly Guid CompanyA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CompanyB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static string Requests => RateLimitAllowanceFields.SettingName("Authenticated", RateLimitAllowanceFields.RequestsPerWindow);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ACompanysCallersAreHeldToItsOwnAllowanceAndAnotherCompanysAreNot()
    {
        _settings.SetForCompany(CompanyA, Requests, "3");
        using var limiter = LimiterFor(RateLimitPolicies.Authenticated);

        RefusedAfterTheAllowance(limiter, () => CallerOfCompany(CompanyA, "a1", "10.0.0.1"), 3).ShouldBeTrue();
        RefusedAfterTheAllowance(limiter, () => CallerOfCompany(CompanyB, "b1", "10.0.0.1"), 5).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ALockedServerNumberHoldsForACompanysCallers()
    {
        _settings.SetForCompany(CompanyA, Requests, "3").Lock(Requests);
        using var limiter = LimiterFor(RateLimitPolicies.Authenticated);

        RefusedAfterTheAllowance(limiter, () => CallerOfCompany(CompanyA, "a1", "10.0.0.1"), 5).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TwoCompaniesHeldToDifferentNumbersDoNotShareASharedBucket()
    {
        TodaysRateLimitSettings.Set(_settings, "Authenticated", RateLimitAllowanceFields.Partitioning, "Shared");
        _settings.SetForCompany(CompanyA, Requests, "2");
        using var limiter = LimiterFor(RateLimitPolicies.Authenticated);

        RefusedAfterTheAllowance(limiter, () => CallerOfCompany(CompanyA, "a1", "10.0.0.1"), 2).ShouldBeTrue();

        // Company B is held to the server's 5, in a bucket of its own: company A using up its 2 did not touch it.
        Acquired(limiter, CallerOfCompany(CompanyB, "b1", "10.0.0.2")).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TwoCompaniesHeldToTheSameNumbersShareASharedBucketAsTheWayOfCountingSays()
    {
        TodaysRateLimitSettings.Set(_settings, "Authenticated", RateLimitAllowanceFields.Partitioning, "Shared");
        using var limiter = LimiterFor(RateLimitPolicies.Authenticated);

        RefusedAfterTheAllowance(limiter, () => CallerOfCompany(CompanyA, "a1", "10.0.0.1"), 5).ShouldBeTrue();

        // Counted as one for everyone and both held to 5, company B's caller meets the bucket company A used up.
        Acquired(limiter, CallerOfCompany(CompanyB, "b1", "10.0.0.2")).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ACompanyClaimThatIsNotACompanyIdIsAFaultNotTheServersAllowance()
    {
        var context = new DefaultHttpContext
        {
            RequestServices = _services,
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "any"),
                    new Claim(ClaimDefinitions.tenantId.Name, "not-a-company"),
                },
                "test")),
        };

        var fault = Should.Throw<InvalidOperationException>(
            () => RateLimitingServiceExtensions.PartitionFor(RateLimitPolicies.Authenticated, context));

        fault.Message.ShouldContain(ClaimDefinitions.tenantId.Name);
    }
}
