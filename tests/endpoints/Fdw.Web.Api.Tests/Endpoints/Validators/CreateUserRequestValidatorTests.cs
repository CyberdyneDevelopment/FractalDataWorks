using Fdw.Services.Users.Clients.Models;
using Fdw.Services.Users.Endpoints.Validators;

namespace Fdw.Web.Api.Tests.Endpoints.Validators;

/// <summary>Tests for the create-user rules every host inherits (FDW-3572).</summary>
public sealed class CreateUserRequestValidatorTests
{
    private sealed class Sut : CreateUserRequestValidator<CreateUserRequest>;

    private static CreateUserRequest Valid() => new()
    {
        Username = "analyst_one",
        SignInMethods = new SignInMethodsRequest { Password = new PasswordSignInMethod { InitialPassword = "correct-horse-1" } },
        Roles = [],
    };

    [Fact]
    public void AValidRequestPasses() => new Sut().Validate(Valid()).IsValid.ShouldBeTrue();

    [Fact]
    public void AnEmailAddressIsAValidUsername()
    {
        var request = Valid();
        request.Username = "analyst.one@example.com";

        new Sut().Validate(request).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AnIdentifierUsernameThatStartsWithADigitIsRefused()
    {
        var request = Valid();
        request.Username = "1analyst";

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "Username");
    }

    [Fact]
    public void AnEmailFormUsernameWithAColonIsRefused()
    {
        var request = Valid();
        request.Username = "analyst:one@example.com";

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "Username");
    }

    [Fact]
    public void ARequestWithNoSignInMethodIsRefused()
    {
        var request = Valid();
        request.SignInMethods = new SignInMethodsRequest();

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "SignInMethods");
    }

    [Fact]
    public void APasswordUnderEightCharactersIsRefused()
    {
        var request = Valid();
        request.SignInMethods.Password!.InitialPassword = "short";

        new Sut().Validate(request).Errors.ShouldContain(e => e.ErrorMessage == "Password must be at least 8 characters");
    }

    [Fact]
    public void ARequestThatDoesNotStateRolesIsRefused()
    {
        var request = Valid();
        request.Roles = null;

        new Sut().Validate(request).Errors.ShouldContain(e => e.PropertyName == "Roles");
    }

    [Fact]
    public void ARoleNameOver100CharactersIsRefused()
    {
        var request = Valid();
        request.Roles = [new string('r', 101)];

        new Sut().Validate(request).Errors.ShouldContain(e => e.ErrorMessage == "Role name cannot exceed 100 characters");
    }
}
