using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Fdw.Web.RestEndpoints.Tests.EndpointTypeOptions;

/// <summary>
/// FDW-4657: an endpoint option declares the services it needs registered, and the endpoint collection's Initialize refuses a host that lacks one, naming each, instead of the first request failing.
/// </summary>
/// <remarks>
/// The same declaration a service type makes (FDW-4588), so these tests state the same rules for the endpoint level: one message per missing service across every routed member, a service two
/// members need reported once, never resolved, a replaced member body still covered, a member the collection does not route not asked.
/// </remarks>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public sealed class EndpointRequirementsTests
{
    private interface IAlpha;

    private interface IBeta;

    private sealed class Alpha : IAlpha;

    private sealed class Beta : IBeta;

    private sealed class SomeEndpoint;

    private sealed class Coded : IGenericMessage
    {
        public Coded(string code) => Code = code;

        public string Message => "missing " + Code;

        public string? Code { get; }

        public string? Source => nameof(EndpointRequirementsTests);
    }

    private static Coded AlphaMissing(ILogger logger) => new Coded("ALPHA-1");

    private static Coded BetaMissing(ILogger logger) => new Coded("BETA-1");

    private sealed class NeedsAlphaOption : EndpointTypeOptionBase
    {
        public NeedsAlphaOption()
            : base("NeedsAlpha", typeof(SomeEndpoint), "Needs the alpha service.", "Test")
        {
            Requires<IAlpha>(AlphaMissing);
        }
    }

    private sealed class NeedsBetaOption : EndpointTypeOptionBase
    {
        public NeedsBetaOption()
            : base("NeedsBeta", typeof(SomeEndpoint), "Needs the beta service.", "Test")
        {
            Requires<IBeta>(BetaMissing);
        }
    }

    private sealed class NeedsBothOption : EndpointTypeOptionBase
    {
        public NeedsBothOption()
            : base("NeedsBoth", typeof(SomeEndpoint), "Needs both services.", "Test")
        {
            Requires<IAlpha>(AlphaMissing);
            Requires<IBeta>(BetaMissing);
            Requires<IAlpha>(AlphaMissing);
        }
    }

    private sealed class NeedsNothingOption : EndpointTypeOptionBase
    {
        public NeedsNothingOption()
            : base("NeedsNothing", typeof(SomeEndpoint), "Needs nothing.", "Test")
        {
        }
    }

    private sealed class TestCollection(IEnumerable<IEndpointTypeOption> members) : EndpointTypeCollectionBase<EndpointTypeOptionBase>
    {
        public override IEnumerable<IEndpointTypeOption> Members { get; } = members;
    }

    private static IHost BuildHost(Action<IServiceCollection>? register = null)
    {
        var builder = Host.CreateApplicationBuilder();
        register?.Invoke(builder.Services);
        return builder.Build();
    }

    private static string[] Codes(IGenericResult<IHost> result) => result.Messages.Select(m => m.Code ?? string.Empty).ToArray();

    private static IGenericResult<IHost> Initialize(IHost host, params IEndpointTypeOption[] members) => new TestCollection(members).Initialize(host);

    /// <summary>A host without the service is refused with the option's own coded message.</summary>
    [Fact]
    public void InitializeFailsNamingTheMissingService()
    {
        var result = Initialize(BuildHost(), new NeedsAlphaOption());

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldBe(["ALPHA-1"]);
    }

    /// <summary>A host that registered the service passes, whatever its lifetime.</summary>
    [Fact]
    public void InitializePassesWhenTheServiceIsRegistered()
    {
        Initialize(BuildHost(services => services.AddScoped<IAlpha, Alpha>()), new NeedsAlphaOption()).IsSuccess.ShouldBeTrue();
    }

    /// <summary>Every missing service of one member is reported in one result, one message each, and a service declared twice once.</summary>
    [Fact]
    public void SeveralMissingServicesGiveOneMessageEachInOneResult()
    {
        var result = Initialize(BuildHost(), new NeedsBothOption());

        result.IsFailure.ShouldBeTrue();
        Codes(result).OrderBy(c => c, StringComparer.Ordinal).ToArray().ShouldBe(["ALPHA-1", "BETA-1"]);
    }

    /// <summary>Members missing different services are all reported in one boot, and one two members need is reported once.</summary>
    [Fact]
    public void TheServicesOfEveryMemberAreReportedTogetherEachOnce()
    {
        var result = Initialize(BuildHost(), new NeedsAlphaOption(), new NeedsBetaOption(), new NeedsBothOption());

        result.IsFailure.ShouldBeTrue();
        Codes(result).OrderBy(c => c, StringComparer.Ordinal).ToArray().ShouldBe(["ALPHA-1", "BETA-1"]);
    }

    /// <summary>The check never builds the service: a registration whose factory throws still passes it.</summary>
    [Fact]
    public void TheCheckNeverResolvesTheService()
    {
        var built = false;

        var result = Initialize(BuildHost(services => services.AddSingleton<IAlpha>(_ =>
        {
            built = true;
            throw new InvalidOperationException("resolved");
        })), new NeedsAlphaOption());

        result.IsSuccess.ShouldBeTrue();
        built.ShouldBeFalse();
    }

    /// <summary>A member whose Initialization body a host replaced is still checked, and its body does not run for a host that lacks the service.</summary>
    [Fact]
    public void AReplacedMemberBodyIsStillChecked()
    {
        var ran = false;
        var option = new NeedsAlphaOption();
        option.Initialization((host, loggerFactory) =>
        {
            ran = true;
            return GenericResult<IHost>.Success(host);
        });

        var result = Initialize(BuildHost(), option);

        result.IsFailure.ShouldBeTrue();
        ran.ShouldBeFalse();
    }

    /// <summary>A member with no requirements passes on any host.</summary>
    [Fact]
    public void AMemberWithNoRequirementsPasses()
    {
        Initialize(BuildHost(), new NeedsNothingOption()).IsSuccess.ShouldBeTrue();
    }

    /// <summary>The failure stops the collection before any member's body runs.</summary>
    [Fact]
    public void TheCollectionStopsBeforeAnyMemberRuns()
    {
        var ran = false;
        var healthy = new NeedsNothingOption();
        healthy.Initialization((host, loggerFactory) =>
        {
            ran = true;
            return GenericResult<IHost>.Success(host);
        });

        var result = Initialize(BuildHost(), healthy, new NeedsAlphaOption());

        result.IsFailure.ShouldBeTrue();
        ran.ShouldBeFalse();
    }

    /// <summary>A member that is not routed (SkipRegistration) is not asked for its services.</summary>
    [Fact]
    public void TheCollectionDoesNotAskAMemberItDoesNotRoute()
    {
        var unrouted = new NeedsAlphaOption { SkipRegistration = true };

        Initialize(BuildHost(), unrouted).IsSuccess.ShouldBeTrue();
    }
}
