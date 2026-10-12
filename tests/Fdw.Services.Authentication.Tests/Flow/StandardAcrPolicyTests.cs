using System.Collections.Generic;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// The standard policy reports the deployment's declared levels, never a value of its own, and refuses
/// a level nobody declared as misconfiguration rather than as a proof that fell short (FDW-954).
/// </summary>
public sealed class StandardAcrPolicyTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void OneFactorKindReachesTheSingleFactorLevel()
    {
        TestAssuranceLevels.Policy().Evaluate(["pwd"]).ShouldBe(TestAssuranceLevels.SingleFactor.AcrValue);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void TwoProofsOfOneKindAreStillOneFactor()
    {
        TestAssuranceLevels.Policy().Evaluate(["pwd", "pin"]).ShouldBe(TestAssuranceLevels.SingleFactor.AcrValue);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void TwoFactorKindsReachTheHighestLevelTheyMeet()
    {
        TestAssuranceLevels.Policy().Evaluate(["pwd", "otp", "fpt"]).ShouldBe(TestAssuranceLevels.MultiFactor.AcrValue);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void NoProofReachesNoLevel()
    {
        TestAssuranceLevels.Policy().Evaluate([]).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void ADeploymentThatDeclaresNoLevelsReportsNone()
    {
        new StandardAcrPolicy(new List<IAssuranceLevel>(), null).Evaluate(["pwd", "otp"]).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void AHigherLevelMeetsALowerRequirement()
    {
        var meets = TestAssuranceLevels.Policy().Meets(TestAssuranceLevels.MultiFactor.AcrValue, TestAssuranceLevels.SingleFactor.AcrValue);

        meets.IsSuccess.ShouldBeTrue();
        meets.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void ALowerLevelDoesNotMeetAHigherRequirement()
    {
        var meets = TestAssuranceLevels.Policy().Meets(TestAssuranceLevels.SingleFactor.AcrValue, TestAssuranceLevels.MultiFactor.AcrValue);

        meets.IsSuccess.ShouldBeTrue();
        meets.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void NoLevelReachedDoesNotMeetARequirement()
    {
        var meets = TestAssuranceLevels.Policy().Meets(null, TestAssuranceLevels.SingleFactor.AcrValue);

        meets.IsSuccess.ShouldBeTrue();
        meets.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void ARequirementNobodyDeclaredIsRefusedAsMisconfigured()
    {
        TestAssuranceLevels.Policy().Meets(TestAssuranceLevels.MultiFactor.AcrValue, "urn:fdw:acr:multi-factor").IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authentication")]
    public void NoRequirementIsMetByAnything()
    {
        var meets = TestAssuranceLevels.Policy().Meets(null, null);

        meets.IsSuccess.ShouldBeTrue();
        meets.Value.ShouldBeTrue();
    }
}
