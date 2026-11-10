using System;
using Fdw.Operations.Configuration;
using Fdw.Operations.Validators;
using Fdw.Validation.Configuration;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Endpoints.Tests;

/// <summary>
/// A write of an escalation level or recipient that its validator refuses is answered with the code of the rule that refused it, not with
/// <c>VALIDATION-20003</c> (FDW-4738). The choices are named, and a refusal of several rules names every one while keeping the first rule's code.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Escalation")]
public sealed class EscalationRefusalAnswerTests
{
    public EscalationRefusalAnswerTests()
    {
        ConfigurationValidators.RegisterMember(new EscalationLevelConfigurationValidation());
        ConfigurationValidators.RegisterMember(new EscalationLevelRecipientConfigurationValidation());
    }

    private static EscalationLevelConfiguration Level(string name, string severity) =>
        new() { Id = Guid.NewGuid(), Name = name, Level = 1, DelayMinutes = 0, NotificationChannel = "Email", Severity = severity };

    [Fact]
    public void ALevelWithAnUnlistedSeverityIsAnswered400WithTheCodeOfThatRule()
    {
        var result = ConfigurationValidatorRunner.Validate(Level("Level1", "Fatal"), NullLogger.Instance);

        var (status, response) = ResultHttpStatusMapper.Map(result, new DefaultHttpContext());

        status.ShouldBe(400);
        response.Extensions["code"].ShouldBe("OPERATIONS-21091");
        response.Detail.ShouldNotBeNull().ShouldContain("'Fatal'");
        response.Detail.ShouldContain("Critical, Error, Info, Warning");
    }

    [Fact]
    public void ALevelWithoutASeverityIsAnswered400WithTheCodeOfThatRule()
    {
        var result = ConfigurationValidatorRunner.Validate(Level("Level1", string.Empty), NullLogger.Instance);

        var (status, response) = ResultHttpStatusMapper.Map(result, new DefaultHttpContext());

        status.ShouldBe(400);
        response.Extensions["code"].ShouldBe("OPERATIONS-21090");
    }

    [Fact]
    public void ARecipientWithAnUnlistedTypeIsAnswered400WithTheCodeOfThatRule()
    {
        var recipient = new EscalationLevelRecipientConfiguration { Id = Guid.NewGuid(), Name = "oncall", Recipient = "oncall@example.com", RecipientType = "Channel" };

        var (status, response) = ResultHttpStatusMapper.Map(ConfigurationValidatorRunner.Validate(recipient, NullLogger.Instance), new DefaultHttpContext());

        status.ShouldBe(400);
        response.Extensions["code"].ShouldBe("OPERATIONS-21093");
        response.Detail.ShouldNotBeNull().ShouldContain("Email, IntegrationKey, Webhook");
    }

    [Fact]
    public void ARefusalOfSeveralRulesKeepsTheCodedRulesCodeAndNamesEveryRule()
    {
        var result = ConfigurationValidatorRunner.Validate(Level(string.Empty, "Fatal"), NullLogger.Instance);

        var (status, response) = ResultHttpStatusMapper.Map(result, new DefaultHttpContext());

        status.ShouldBe(400);
        response.Extensions["code"].ShouldBe("OPERATIONS-21091");
        response.Detail.ShouldNotBeNull().ShouldContain("'Fatal'");
        response.Detail.ShouldContain("Name: Name is required");
    }
}
