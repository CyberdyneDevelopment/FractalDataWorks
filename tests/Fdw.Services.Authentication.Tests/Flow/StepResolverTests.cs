using System;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// Flow validation runs at startup, so these are the failures an operator should see at boot rather
/// than a user should hit at login.
/// </summary>
public sealed class StepResolverTests
{
    private static AuthenticationFlow Flow(params string[] steps) => new()
    {
        Name = "test-flow",
        Steps = steps,
        Audience = "test-audience",
    };

    [Fact]
    public void TwoPackagesClaimingOneNameIsRefused()
    {
        var resolver = new AuthenticationStepResolver();

        var first = resolver.Register("Oidc", new HostileStep { Contributes = [ContextElement.Subject] });
        var second = resolver.Register("Oidc", new HostileStep { Contributes = [ContextElement.Subject] });

        // last-wins would make the same flow mean different things depending on assembly load order
        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void FlowNamingAnUnregisteredStepFailsValidation()
    {
        var resolver = new AuthenticationStepResolver();
        resolver.Register("Oidc", new HostileStep { Contributes = [ContextElement.Subject] });

        // the usual cause is a package reference that was removed
        var result = resolver.Validate(Flow("Oidc", "Saml"));

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void FlowOrderingAStepBeforeItsRequirementFailsValidation()
    {
        var resolver = new AuthenticationStepResolver();
        resolver.Register("Authorize", new HostileStep
        {
            Requires = [ContextElement.Principal],
            Contributes = [ContextElement.Decision],
        });
        resolver.Register("Resolve", new HostileStep
        {
            Requires = [ContextElement.Subject],
            Contributes = [ContextElement.Principal],
        });

        // Authorize needs a Principal that Resolve has not contributed yet
        var result = resolver.Validate(Flow("Authorize", "Resolve"));

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void CorrectlyOrderedFlowValidates()
    {
        var resolver = new AuthenticationStepResolver();
        resolver.Register("Prove", new HostileStep { Contributes = [ContextElement.Subject] });
        resolver.Register("Resolve", new HostileStep
        {
            Requires = [ContextElement.Subject],
            Contributes = [ContextElement.Principal],
        });
        resolver.Register("Authorize", new HostileStep
        {
            Requires = [ContextElement.Principal],
            Contributes = [ContextElement.Decision],
        });

        resolver.Validate(Flow("Prove", "Resolve", "Authorize")).IsSuccess.ShouldBeTrue();
    }
}
