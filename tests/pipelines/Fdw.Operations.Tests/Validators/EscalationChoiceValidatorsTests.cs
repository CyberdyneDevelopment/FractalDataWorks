using Fdw.Operations.Abstractions.TypeCollections.Escalation;
using Fdw.Operations.Configuration;
using Fdw.Operations.Validators;

namespace Fdw.Operations.Tests.Validators;

/// <summary>Tests for the severity of an escalation level and the type of a level recipient (FDW-4693): every value of the collection passes, and a value left out or outside it is refused with the code of its own reason.</summary>
public sealed class EscalationChoiceValidatorsTests
{
    private const string SeverityNotStatedCode = "OPERATIONS-21090";
    private const string SeverityUnknownCode = "OPERATIONS-21091";
    private const string RecipientTypeNotStatedCode = "OPERATIONS-21092";
    private const string RecipientTypeUnknownCode = "OPERATIONS-21093";

    private static EscalationLevelConfiguration Level(string severity) =>
        new() { Id = Guid.NewGuid(), Name = "Level1", Level = 1, DelayMinutes = 0, NotificationChannel = "Email", Severity = severity };

    private static EscalationLevelRecipientConfiguration Recipient(string recipientType) =>
        new() { Id = Guid.NewGuid(), Name = "oncall", Recipient = "oncall@example.com", RecipientType = recipientType };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Escalation")]
    public void TheSeveritiesAreInfoWarningErrorAndCritical()
    {
        EscalationSeverities.All().Select(option => option.Name).ShouldBe(["Info", "Warning", "Error", "Critical"], ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Escalation")]
    public void TheRecipientTypesAreEmailWebhookAndIntegrationKey()
    {
        EscalationRecipientTypes.All().Select(option => option.Name).ShouldBe(["Email", "Webhook", "IntegrationKey"], ignoreOrder: true);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Escalation")]
    [InlineData("Info")]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("Critical")]
    public void ALevelWithAListedSeverityPasses(string severity)
    {
        new EscalationLevelConfigurationValidator().Validate(Level(severity)).IsValid.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Escalation")]
    [InlineData("")]
    [InlineData("   ")]
    public void ALevelWithoutASeverityIsRefusedWithItsOwnCode(string severity)
    {
        var result = new EscalationLevelConfigurationValidator().Validate(Level(severity));

        result.Errors.Count.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Severity");
        result.Errors[0].ErrorCode.ShouldBe(SeverityNotStatedCode);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Escalation")]
    [InlineData("Fatal")]
    [InlineData("info")]
    public void ALevelWithAnUnlistedSeverityIsRefusedWithItsOwnCodeNamingTheChoices(string severity)
    {
        var result = new EscalationLevelConfigurationValidator().Validate(Level(severity));

        result.Errors.Count.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Severity");
        result.Errors[0].ErrorCode.ShouldBe(SeverityUnknownCode);
        result.Errors[0].ErrorMessage.ShouldContain($"'{severity}'");
        result.Errors[0].ErrorMessage.ShouldContain("Critical, Error, Info, Warning");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Escalation")]
    [InlineData("Email")]
    [InlineData("Webhook")]
    [InlineData("IntegrationKey")]
    public void ARecipientWithAListedTypePasses(string recipientType)
    {
        new EscalationLevelRecipientConfigurationValidator().Validate(Recipient(recipientType)).IsValid.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Escalation")]
    [InlineData("")]
    [InlineData("   ")]
    public void ARecipientWithoutATypeIsRefusedWithItsOwnCode(string recipientType)
    {
        var result = new EscalationLevelRecipientConfigurationValidator().Validate(Recipient(recipientType));

        result.Errors.Count.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("RecipientType");
        result.Errors[0].ErrorCode.ShouldBe(RecipientTypeNotStatedCode);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Escalation")]
    [InlineData("Channel")]
    [InlineData("email")]
    public void ARecipientWithAnUnlistedTypeIsRefusedWithItsOwnCodeNamingTheChoices(string recipientType)
    {
        var result = new EscalationLevelRecipientConfigurationValidator().Validate(Recipient(recipientType));

        result.Errors.Count.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("RecipientType");
        result.Errors[0].ErrorCode.ShouldBe(RecipientTypeUnknownCode);
        result.Errors[0].ErrorMessage.ShouldContain($"'{recipientType}'");
        result.Errors[0].ErrorMessage.ShouldContain("Email, IntegrationKey, Webhook");
    }
}
