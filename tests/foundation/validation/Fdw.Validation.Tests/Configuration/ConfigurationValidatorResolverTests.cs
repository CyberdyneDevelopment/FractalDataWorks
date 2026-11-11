using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Validation.Tests.Configuration;

public abstract class ResolverBaseConfig { public string Name { get; set; } = string.Empty; }
public sealed class ResolverDerivedConfig : ResolverBaseConfig { }
public sealed class ResolverOwnConfig : ResolverBaseConfig { }
public interface IResolverContract { }
public interface IResolverOtherContract { }
public sealed class ResolverContractConfig : IResolverContract { }
public sealed class ResolverTwoContractsConfig : IResolverContract, IResolverOtherContract { }
public sealed class ResolverUnknownConfig { }

public sealed class ResolverBaseValidator : AbstractValidator<ResolverBaseConfig>
{
    public ResolverBaseValidator() => RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
}

public sealed class ResolverContractValidator : AbstractValidator<IResolverContract> { }
public sealed class ResolverOtherContractValidator : AbstractValidator<IResolverOtherContract> { }

[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ResolverBaseConfig")]
public sealed class ResolverBaseOption : ValidatedConfiguration<ResolverBaseConfig>
{
    public ResolverBaseOption() : base("ResolverBaseConfig", new ResolverBaseValidator()) { }
}

[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ResolverOwnConfig")]
public sealed class ResolverOwnOption : UnvalidatedConfiguration<ResolverOwnConfig>
{
    public ResolverOwnOption() : base("ResolverOwnConfig", "its own record states nothing to check") { }
}

[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "IResolverContract")]
public sealed class ResolverContractOption : ValidatedConfiguration<IResolverContract>
{
    public ResolverContractOption() : base("IResolverContract", new ResolverContractValidator()) { }
}

[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "IResolverOtherContract")]
public sealed class ResolverOtherContractOption : ValidatedConfiguration<IResolverOtherContract>
{
    public ResolverOtherContractOption() : base("IResolverOtherContract", new ResolverOtherContractValidator()) { }
}

/// <summary>
/// The validator option that speaks for a configuration class is found by the class, then its bases nearest first, then its interfaces;
/// finding none or several is a coded refusal, never a pass (FDW-3541).
/// </summary>
public sealed class ConfigurationValidatorResolverTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AClassFindsItsOwnOptionBeforeItsBaseClasses()
    {
        var found = ConfigurationValidatorResolver.Find(typeof(ResolverOwnConfig), NullLogger.Instance);

        found.IsSuccess.ShouldBeTrue();
        found.Value!.Name.ShouldBe("ResolverOwnConfig");
        found.Value.NoValidationReason.ShouldNotBeNull();
        found.Value.Validator.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ASubclassFindsTheOptionOfItsBaseClassAndTheBaseValidatorChecksIt()
    {
        var found = ConfigurationValidatorResolver.Find(typeof(ResolverDerivedConfig), NullLogger.Instance);

        found.IsSuccess.ShouldBeTrue();
        found.Value!.Name.ShouldBe("ResolverBaseConfig");
        found.Value.NoValidationReason.ShouldBeNull();
        var refused = found.Value.Validator.ShouldNotBeNull().Validate(new ValidationContext<object>(new ResolverDerivedConfig()));
        refused.IsValid.ShouldBeFalse();
        refused.Errors.Single().ErrorMessage.ShouldBe("Name is required");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AClassFindsTheOptionOfTheSingleInterfaceItImplements()
    {
        var found = ConfigurationValidatorResolver.Find(typeof(ResolverContractConfig), NullLogger.Instance);

        found.IsSuccess.ShouldBeTrue();
        found.Value!.Name.ShouldBe("IResolverContract");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AClassWithOptionsForTwoOfItsInterfacesIsRefusedAndBothAreNamed()
    {
        var found = ConfigurationValidatorResolver.Find(typeof(ResolverTwoContractsConfig), NullLogger.Instance);

        found.IsFailure.ShouldBeTrue();
        found.Messages[^1].Code.ShouldBe("VALIDATION-60002");
        found.CurrentMessage.ShouldNotBeNull().ShouldContain("IResolverContract, IResolverOtherContract");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AClassNoOptionSpeaksForIsRefusedWithACodedMessageNeverPassed()
    {
        var found = ConfigurationValidatorResolver.Find(typeof(ResolverUnknownConfig), NullLogger.Instance);

        found.IsFailure.ShouldBeTrue();
        found.Messages[^1].Code.ShouldBe("VALIDATION-60001");
        found.CurrentMessage.ShouldNotBeNull().ShouldContain("ResolverUnknownConfig");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AnUnvalidatedOptionNeedsAReason()
    {
        Should.Throw<ArgumentException>(() => new BlankReasonOption());
    }

    private sealed class BlankReasonOption : UnvalidatedConfiguration<ResolverOwnConfig>
    {
        public BlankReasonOption() : base("Blank", " ") { }
    }
}

/// <summary>
/// A configuration is checked by the validator its class declares before it is written: valid passes, a refused one is one coded failure naming
/// every failed rule, a class that states no validation passes, and an undeclared class is refused (FDW-3541).
/// </summary>
public sealed class ConfigurationValidatorRunnerTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AValidConfigurationPasses()
    {
        ConfigurationValidatorRunner.Validate(new ResolverDerivedConfig { Name = "x" }, NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ARefusedConfigurationIsOneClientMessageNamingTheRule()
    {
        var result = ConfigurationValidatorRunner.Validate(new ResolverDerivedConfig(), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("VALIDATION-20003");
        result.Messages.OfType<IClientMessage>().ShouldHaveSingleItem().ClientText.ShouldContain("Name: Name is required");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AClassThatStatesNoValidationPasses()
    {
        ConfigurationValidatorRunner.Validate(new ResolverOwnConfig(), NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AClassWithNoDeclarationIsRefusedNotPassed()
    {
        var result = ConfigurationValidatorRunner.Validate(new ResolverUnknownConfig(), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("VALIDATION-60001");
    }
}
