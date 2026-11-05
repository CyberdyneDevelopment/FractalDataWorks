using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Validation.Tests;

public sealed class ValidationResultExtensionsTests
{
    private static readonly ILogger Logger = NullLogger.Instance;

    private static ValidationResult Refused(params (string Property, string Error)[] errors)
        => new(errors.Select(error => new ValidationFailure(error.Property, error.Error)).ToList());

    // ToGenericResult (non-generic) tests
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToGenericResultReturnsSuccessWhenValid()
    {
        var result = new ValidationResult().ToGenericResult(Logger);

        result.IsSuccess.ShouldBeTrue();
        result.Messages.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToGenericResultNamesEveryFailedRuleInOneRefusal()
    {
        var result = Refused(("Name", "Name is required"), ("Email", "Email is invalid")).ToGenericResult(Logger);

        result.IsFailure.ShouldBeTrue();
        var message = result.Messages.Single();
        message.Message.ShouldContain("Name: Name is required");
        message.Message.ShouldContain("Email: Email is invalid");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToGenericResultCarriesANumberInTheValidationBand()
    {
        var result = Refused(("Name", "is required")).ToGenericResult(Logger);

        var message = result.Messages.Single();
        message.Code.ShouldBe("VALIDATION-20001");
        message.ShouldBeAssignableTo<IClientMessage>().ClientText.ShouldNotBeNull().ShouldContain("Name: is required");
    }

    // ToGenericResult<T> (generic) tests
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToGenericResultReturnsSuccessWithValue()
    {
        var result = new ValidationResult().ToGenericResult("TestValue", Logger);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("TestValue");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToGenericResultReturnsFailureWithoutValue()
    {
        var result = Refused(("Name", "is required")).ToGenericResult("TestValue", Logger);

        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => { var _ = result.Value; });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericToGenericResultNamesEveryFailedRuleAndCarriesTheNumber()
    {
        var result = Refused(("Name", "Name is required"), ("Port", "Port must be greater than 0")).ToGenericResult("TestValue", Logger);

        result.IsFailure.ShouldBeTrue();
        var message = result.Messages.Single();
        message.Code.ShouldBe("VALIDATION-20001");
        message.Message.ShouldContain("Name: Name is required");
        message.Message.ShouldContain("Port: Port must be greater than 0");
    }
}
