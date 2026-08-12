using System;
using System.Collections.Generic;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace Fdw.Web.RestEndpoints.Tests.EndpointTypeOptions;

/// <summary>
/// Tests that the switch actually switches, at the option and at the collection.
/// </summary>
/// <remarks>
/// The whole reason endpoints are declared rather than scanned for is that a scan cannot be
/// switched off. If SkipRegistration is set and the endpoint still registers, the mechanism has
/// bought nothing over the assembly scanning it replaced — and the failure is silent, because a
/// registered endpoint looks identical to an intended one.
/// </remarks>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public sealed class SkipRegistrationTests
{
    private sealed class AlphaEndpoint;

    private sealed class BetaEndpoint;

    private sealed class TestEndpointOption(Type endpointType, string name)
        : EndpointTypeOptionBase(name, endpointType, $"The {name} endpoint.", "Test");

    private sealed class TestCollection(IEnumerable<IEndpointTypeOption> members)
        : EndpointTypeCollectionBase<EndpointTypeOptionBase>
    {
        public override IEnumerable<IEndpointTypeOption> Members { get; } = members;
    }

    private static IHostApplicationBuilder NewBuilder() => Host.CreateApplicationBuilder();

    /// <summary>An option not skipped puts its endpoint in the container.</summary>
    [Fact]
    public void RegisterAddsTheEndpointToTheContainer()
    {
        var builder = NewBuilder();

        new TestEndpointOption(typeof(AlphaEndpoint), "Alpha").Register(builder);

        builder.Services.ShouldContain(d => d.ServiceType == typeof(AlphaEndpoint));
    }

    /// <summary>An option not skipped declares its endpoint for routing.</summary>
    /// <remarks>
    /// The second half of registering: DI makes the endpoint constructible, DeclaredEndpoints makes
    /// FastEndpoints willing to route it. Either alone leaves an endpoint that cannot serve.
    /// </remarks>
    [Fact]
    public void RegisterDeclaresTheEndpointForRouting()
    {
        new TestEndpointOption(typeof(BetaEndpoint), "Beta").Register(NewBuilder());

        DeclaredEndpoints.IsDeclared(typeof(BetaEndpoint)).ShouldBeTrue();
    }

    /// <summary>A collection registers the members that are not skipped.</summary>
    [Fact]
    public void CollectionRegistersSelectedMembers()
    {
        var builder = NewBuilder();
        var collection = new TestCollection([new TestEndpointOption(typeof(AlphaEndpoint), "Alpha")]);

        collection.Register(builder);

        builder.Services.ShouldContain(d => d.ServiceType == typeof(AlphaEndpoint));
    }

    /// <summary>A skipped member is passed over while its siblings register.</summary>
    [Fact]
    public void CollectionSkipsTheSkippedMember()
    {
        var builder = NewBuilder();
        var skipped = new TestEndpointOption(typeof(BetaEndpoint), "BetaSkipped") { SkipRegistration = true };
        var kept = new TestEndpointOption(typeof(AlphaEndpoint), "AlphaKept");

        new TestCollection([skipped, kept]).Register(builder);

        builder.Services.ShouldNotContain(d => d.ServiceType == typeof(BetaEndpoint));
        builder.Services.ShouldContain(d => d.ServiceType == typeof(AlphaEndpoint));
    }

    /// <summary>A skipped collection registers none of its members.</summary>
    /// <remarks>
    /// The outer of the two levels: switching a resource off must not require switching off each
    /// endpoint within it.
    /// </remarks>
    [Fact]
    public void SkippedCollectionRegistersNothing()
    {
        var builder = NewBuilder();
        var before = builder.Services.Count;
        var collection = new TestCollection([new TestEndpointOption(typeof(AlphaEndpoint), "Alpha")])
        {
            SkipRegistration = true,
        };

        collection.Register(builder);

        builder.Services.Count.ShouldBe(before);
    }

    /// <summary>A collection with no members registers nothing and does not throw.</summary>
    /// <remarks>
    /// Nothing to register is a real state, not a mistake — a domain whose endpoints are all
    /// switched off, or which has none yet, says so by doing nothing.
    /// </remarks>
    [Fact]
    public void EmptyCollectionRegistersNothingWithoutThrowing()
    {
        var builder = NewBuilder();
        var before = builder.Services.Count;

        new TestCollection([]).Register(builder).IsSuccess.ShouldBeTrue();

        builder.Services.Count.ShouldBe(before);
    }
}
