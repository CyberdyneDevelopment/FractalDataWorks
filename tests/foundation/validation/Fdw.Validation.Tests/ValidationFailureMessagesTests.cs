using FluentValidation;
using FluentValidation.Results;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Validation.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Validation.Tests;

/// <summary>A configuration with a rule that has no coded message and one that has its own (FDW-4738).</summary>
public sealed class CodedRefusalConfig
{
    public string Name { get; set; } = string.Empty;

    public string Choice { get; set; } = string.Empty;
}

/// <summary>Refuses an empty <c>Name</c> with plain text and every <c>Choice</c> with a coded message in its custom state.</summary>
public sealed class CodedRefusalValidator : AbstractValidator<CodedRefusalConfig>
{
    public CodedRefusalValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(x => x.Choice).Custom((_, context) => context.AddFailure(CodedFailure()));
    }

    public static IGenericMessage CodedMessage() => ConfigurationValidatorLog.NoValidatorDeclared(NullLogger.Instance, "Choice");

    private static ValidationFailure CodedFailure()
    {
        var message = CodedMessage();
        return new ValidationFailure("Choice", message.Message) { ErrorCode = message.Code, CustomState = message };
    }
}

/// <summary>Declares the validator of <see cref="CodedRefusalConfig"/>.</summary>
public sealed class CodedRefusalOption : ValidatedConfiguration<CodedRefusalConfig>
{
    public CodedRefusalOption() : base("CodedRefusalConfig", new CodedRefusalValidator()) { }
}

/// <summary>
/// A refusal keeps the code of every rule that has a coded message of its own: the validator runner and <c>ToGenericResult</c> pass it through as
/// itself and wrap only the other rules (FDW-4738). It was joined with them into one 20003 or 20001 message, so the code was lost.
/// </summary>
public sealed class ValidationFailureMessagesTests
{
    private static ValidationFailure Coded(IGenericMessage message) =>
        new("Choice", message.Message) { ErrorCode = message.Code, CustomState = message };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ARuleWithACodedMessageIsPassedThroughAsThatMessage()
    {
        var coded = CodedRefusalValidator.CodedMessage();

        var messages = ValidationFailureMessages.From(new ValidationResult([Coded(coded)]), details => ValidationLog.InputRejected(NullLogger.Instance, details));

        messages.ShouldHaveSingleItem().ShouldBeSameAs(coded);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheRulesWithoutACodedMessageAreWrappedInOneMessageAfterTheCodedOnes()
    {
        var coded = CodedRefusalValidator.CodedMessage();
        var refused = new ValidationResult([new ValidationFailure("Name", "Name is required"), Coded(coded), new ValidationFailure("Port", "Port must be greater than 0")]);

        var messages = ValidationFailureMessages.From(refused, details => ValidationLog.InputRejected(NullLogger.Instance, details));

        messages.Count.ShouldBe(2);
        messages[0].ShouldBeSameAs(coded);
        messages[1].Code.ShouldBe("VALIDATION-20001");
        messages[1].Message.ShouldContain("Name: Name is required; Port: Port must be greater than 0");
        messages[1].Message.ShouldNotContain("Choice");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ACustomStateThatIsNotAFailureMessageIsWrappedWithTheOtherRules()
    {
        var progress = GenericMessage.Create(MessageSeverity.Information, "progress", "TEST-11000", null);
        var refused = new ValidationResult([new ValidationFailure("Choice", "not allowed") { CustomState = progress }]);

        var messages = ValidationFailureMessages.From(refused, details => ValidationLog.InputRejected(NullLogger.Instance, details));

        messages.ShouldHaveSingleItem().Code.ShouldBe("VALIDATION-20001");
        messages[0].Message.ShouldContain("Choice: not allowed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToGenericResultCarriesTheCodedMessageOfARule()
    {
        var coded = CodedRefusalValidator.CodedMessage();
        var refused = new ValidationResult([Coded(coded), new ValidationFailure("Name", "Name is required")]);

        var result = refused.ToGenericResult(NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(message => message.Code).ShouldBe([coded.Code, "VALIDATION-20001"]);
        result.FirstFailureMessage()!.Code.ShouldBe(coded.Code);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheRunnerAnswersARefusedConfigurationWithTheCodedMessageThenOneMessageForTheOtherRules()
    {
        var result = ConfigurationValidatorRunner.Validate(new CodedRefusalConfig(), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(message => message.Code).ShouldBe([CodedRefusalValidator.CodedMessage().Code, "VALIDATION-20003"]);
        result.Messages[^1].Message.ShouldContain("Name: Name is required");
        result.Messages[^1].Message.ShouldNotContain("Choice");
        result.FirstFailureMessage()!.Code.ShouldBe(CodedRefusalValidator.CodedMessage().Code);
    }
}
