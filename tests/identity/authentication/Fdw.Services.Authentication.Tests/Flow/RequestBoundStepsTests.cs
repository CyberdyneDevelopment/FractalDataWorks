using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// Tests that a step needing request services gets the ones of the request that runs it (FDW-4640): two logins in two scopes each get their own services, the step in the
/// collection is never changed, and a step that needs none is returned as it is.
/// </summary>
public sealed class RequestBoundStepsTests
{
    private sealed class Carrier
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed class NeedsRequest : IAuthenticationStep, IRequestBoundAuthenticationStep
    {
        public Carrier? Held { get; private init; }

        public string Name => "NeedsRequest";

        public IReadOnlyList<IContextElement> Requires => [];

        public IReadOnlyList<IContextElement> Contributes => [];

        public IReadOnlyList<string> AuthenticationMethods => [];

        public Type RequestServicesType => typeof(Carrier);

        public IAuthenticationStep BindToRequest(object services) => new NeedsRequest { Held = (Carrier)services };

        public Task<IGenericResult<StepOutcome>> Execute(AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class NeedsNothing : IAuthenticationStep
    {
        public string Name => "NeedsNothing";

        public IReadOnlyList<IContextElement> Requires => [];

        public IReadOnlyList<IContextElement> Contributes => [];

        public IReadOnlyList<string> AuthenticationMethods => [];

        public Task<IGenericResult<StepOutcome>> Execute(AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static ServiceProvider Container()
    {
        var services = new ServiceCollection();
        services.AddScoped<Carrier>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    [Fact]
    public void TwoLoginsInTwoScopesEachGetTheirOwnServices()
    {
        var step = new NeedsRequest();
        using var container = Container();
        using var first = container.CreateScope();
        using var second = container.CreateScope();

        var forFirst = (NeedsRequest)RequestBoundSteps.ForRequest(step, type => first.ServiceProvider.GetRequiredService(type))!;
        var forSecond = (NeedsRequest)RequestBoundSteps.ForRequest(step, type => second.ServiceProvider.GetRequiredService(type))!;

        forFirst.Held.ShouldBe(first.ServiceProvider.GetRequiredService<Carrier>());
        forSecond.Held.ShouldBe(second.ServiceProvider.GetRequiredService<Carrier>());
        forFirst.Held.ShouldNotBeSameAs(forSecond.Held);
    }

    [Fact]
    public void TheStepInTheCollectionIsNeverChanged()
    {
        var step = new NeedsRequest();
        using var container = Container();
        using var scope = container.CreateScope();

        var bound = RequestBoundSteps.ForRequest(step, type => scope.ServiceProvider.GetRequiredService(type));

        bound.ShouldNotBeSameAs(step);
        step.Held.ShouldBeNull();
    }

    [Fact]
    public void ALoginInOneScopeKeepsItsServicesWhenAnotherScopeBinds()
    {
        var step = new NeedsRequest();
        using var container = Container();
        using var first = container.CreateScope();
        var forFirst = (NeedsRequest)RequestBoundSteps.ForRequest(step, type => first.ServiceProvider.GetRequiredService(type))!;
        var held = forFirst.Held;

        using var second = container.CreateScope();
        _ = RequestBoundSteps.ForRequest(step, type => second.ServiceProvider.GetRequiredService(type));

        forFirst.Held.ShouldBeSameAs(held);
    }

    [Fact]
    public void AStepThatNeedsNothingIsReturnedAsItIs()
    {
        var step = new NeedsNothing();

        RequestBoundSteps.ForRequest(step, _ => throw new InvalidOperationException("Nothing should be resolved for a step that needs nothing.")).ShouldBeSameAs(step);
    }

    [Fact]
    public void NoStepStaysNoStep()
    {
        RequestBoundSteps.ForRequest(null, _ => throw new InvalidOperationException("Nothing should be resolved when no step answered.")).ShouldBeNull();
    }
}
